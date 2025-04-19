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
    
    public async Task<bool> Register(UserModel userModel, CancellationToken cancellationToken)
    {
        if (await _userCollection
                .Find(x => x.Email == userModel.Email)
                .AnyAsync(cancellationToken))
        {
            Logger.Error($"[AuthService.Register] Email ({userModel.Email}) already registered");
            throw new AppException("User already exists", 409);
        }
        
        await InitialModelUpdate(userModel);
        //fluent validation yapılmalı
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
            Logger.Error("[AuthService.Login] Invalid password");
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
        userModel.ProfilePhoto = "0";
        userModel.Roles ??= [];
        userModel.Devices ??= [];
        userModel.Roles.Add("user");
        userModel.Devices.Add(currentUserService.GetDeviceId());
        userModel.WatchTime = 0;
        return Task.CompletedTask;
    }
}