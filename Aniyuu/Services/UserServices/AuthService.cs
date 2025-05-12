using System.Security.Cryptography;
using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Interfaces;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models;
using Aniyuu.Models.UserModels;
using Aniyuu.Utils;
using MongoDB.Driver;
using NLog;

namespace Aniyuu.Services.UserServices;

public class AuthService(
    ICurrentUserService currentUserService, 
    IMongoDbContext mongoDbContext, 
    ITokenService tokenService, 
    IEmailService emailService,
    IActivationService activationService) : IAuthService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IMongoCollection<UserModel> _userCollection = mongoDbContext
        .GetCollection<UserModel>(AppSettingConfig.Configuration["MongoDBSettings:UserCollection"]!);
    private readonly IMongoCollection<PasswordTokenModel> _passTokenCollection = mongoDbContext
        .GetCollection<PasswordTokenModel>(AppSettingConfig.Configuration["MongoDBSettings:PasswordTokenCollection"]!);
    
    public async Task<bool> Register(UserModel userModel, CancellationToken cancellationToken)
    {
        if (await _userCollection
                .Find(x => x.Email == userModel.Email && x.IsDeleted == false)
                .AnyAsync(cancellationToken))
        {
            Logger.Error($"[AuthService.Register] Email ({userModel.Email}) already registered");
            throw new AppException("User already exists", 409);
        }
        
        await InitialModelUpdate(userModel);
        //fluent validation yapılmalı
        try
        {
            await _userCollection.InsertOneAsync(userModel, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error($"[AuthService.Register] Error during registration: {e.Message}");
            throw new AppException("Error during registration", 500);
        }
        var code = await activationService.GenerateActivationCode(userModel.Email, cancellationToken);
        emailService.SendWelcomeEmail(userModel.Email, userModel.Username, code ,cancellationToken);
        
        return true;
    }

    public async Task<TokensModel> Login(string email, string password, CancellationToken cancellationToken)
    {
        var deviceId = currentUserService.GetDeviceId();
        var user = await _userCollection
            .Find(x => x.Email == email &&
                       x.IsDeleted == false)
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            Logger.Error($"[AuthService.Login] User {email} not found");
            throw new AppException("User not found", 404);
        }

        if (user.IsBanned == true)
        {
            Logger.Error($"[AuthService.Login] User {user.Id} is banned");
            throw new AppException("User is banned", 401);
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.HashedPassword))
        {
            Logger.Error($"[AuthService.Login] Invalid password. User:{user.Id}");
            throw new AppException("Password is incorrect");
        }

        var accessToken = await tokenService.GenerateAccessToken(user.Id, cancellationToken);
        var refreshToken = await tokenService.GenerateRefreshToken(user.Id, deviceId, cancellationToken);

        if (!user.Devices.Contains(deviceId))
        {
            var filter = Builders<UserModel>.Filter.Eq(x => x.Email, email);
            var update = Builders<UserModel>.Update.AddToSet(x => x.Devices, deviceId);
            await _userCollection.UpdateOneAsync(filter, update, null, cancellationToken);
        }
        
        emailService.NewDeviceLoginEmail(user.Email, user.Username, cancellationToken);
        
        return new TokensModel
        {
            RefreshToken = refreshToken,
            AccessToken = accessToken
        };
    }

    public async Task<bool> Logout(CancellationToken cancellationToken)
    {
        await tokenService.RevokeAllTokens(currentUserService.GetUserId(),currentUserService.GetDeviceId(), cancellationToken);
        return true;
    }

    public async Task<string> SendPasswordRecovery(string email, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userCollection.Find(x => x.Email == email && x.IsDeleted == false)
                .FirstOrDefaultAsync(cancellationToken);

            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            var passResetToken = new PasswordTokenModel()
            {
                UserId = user.Id,
                PasswordToken = Convert.ToBase64String(randomNumber),
                Expiration = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };

            await _passTokenCollection.InsertOneAsync(passResetToken, cancellationToken);
            emailService.PasswordResetEmail(email, user.Username, passResetToken.PasswordToken, cancellationToken);

            return $"Password reset email has been sent to {email}";
        }
        catch (Exception e)
        {
            Logger.Error($"[AuthService.PasswordRecovery] Error during sending password reset email: {e.Message}");
            throw new AppException("Error during sending password reset email", 500);
        }
    }

    public async Task<string> RecoverPassword(string passToken, string newPassword, string newPasswordAgain, CancellationToken cancellationToken)
    {
        var filter = Builders<PasswordTokenModel>.Filter.And(
            Builders<PasswordTokenModel>.Filter.Eq(x => x.PasswordToken, passToken),
            Builders<PasswordTokenModel>.Filter.Eq(x => x.IsUsed, false));
            
        var tokenModel = await _passTokenCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (tokenModel == null)
        {
            Logger.Error($"[AuthService.RecoverPassword] Token not found");
            throw new AppException("Token not found", 404);
        }

        if (tokenModel.Expiration < DateTime.UtcNow)
        {
            var update = Builders<PasswordTokenModel>.Update.Set(x => x.IsUsed, true);
            try
            {
                await _passTokenCollection.UpdateOneAsync(filter, update, null, cancellationToken);
            }
            catch (Exception e)
            {
                Logger.Error($"[AuthService.RecoverPassword] Token update failed. user:{tokenModel.PasswordToken}");
                throw new AppException("Token update failed", 500);
            }
            Logger.Error($"[AuthService.RecoverPassword] Token has been expired. token:{passToken}");
            throw new AppException("Token has been expired", 401);
        }

        if (newPassword != newPasswordAgain)
        {
            Logger.Error($"[AuthService.RecoverPassword] Passwords do not match");
            throw new AppException("Passwords do not match", 401);
        }
        var userFilter = Builders<UserModel>.Filter.And(
            Builders<UserModel>.Filter.Eq(x => x.Id, tokenModel.UserId),
            Builders<UserModel>.Filter.Eq(x => x.IsDeleted, false));
        
        var user = await _userCollection.Find(userFilter).FirstOrDefaultAsync(cancellationToken);
        user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword, 12);
        var userUpdate = Builders<UserModel>.Update.Set(x => x.HashedPassword, user.HashedPassword);
        
        try
        {
            await _userCollection.UpdateOneAsync(userFilter, userUpdate, null, cancellationToken);
        }
        catch (Exception e)
        {
           Logger.Error($"[AuthService.RecoverPassword] User update failed. user:{tokenModel.UserId}");
           throw new AppException("User update failed", 500);
        }
        
        try
        {
            var update = Builders<PasswordTokenModel>.Update.Set(x => x.IsUsed, true);
            await _passTokenCollection.UpdateOneAsync(filter, update, null, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error($"[AuthService.RecoverPassword] Token update failed. user:{tokenModel.PasswordToken}");
            throw new AppException("Token update failed", 500);
        }
        return $"User {user.Username} password has been recovered";
    }

    private Task InitialModelUpdate(UserModel userModel)
    {
        userModel.Gender = "not specified";
        userModel.IsActive = false;
        userModel.IsBanned = false;
        userModel.IsDeleted = false;
        userModel.HashedPassword = BCrypt.Net.BCrypt.HashPassword(userModel.HashedPassword, 12);
        userModel.CreatedAt = DateTime.UtcNow;
        userModel.UpdatedAt = DateTime.UtcNow;
        userModel.UpdatedBy = "system";
        userModel.ProfilePhoto = "https://aniyuupfp.blob.core.windows.net/profilephotos/default.jpg";
        userModel.Devices ??= [];
        userModel.Devices.Add(currentUserService.GetDeviceId());
        userModel.WatchTime = 0;
        return Task.CompletedTask;
    }
}