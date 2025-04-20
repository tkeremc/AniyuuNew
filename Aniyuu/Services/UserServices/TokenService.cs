using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aniyuu.DbContext;
using Aniyuu.Exceptions;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models;
using Aniyuu.Models.UserModels;
using Aniyuu.Utils;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using NLog;

namespace Aniyuu.Services.UserServices;

public class TokenService(IMongoDbContext mongoDbContext,
    ICurrentUserService currentUserService) :  ITokenService
{
    private readonly string? _secretKey = AppSettingConfig.Configuration["JwtSettings:SecretKey"];
    private readonly string? _audience = AppSettingConfig.Configuration["JwtSettings:Audience"];
    private readonly string? _issuer = AppSettingConfig.Configuration["JwtSettings:Issuer"];
    private readonly string? _accessTokenTime = AppSettingConfig.Configuration["JwtSettings:AccessTokenTime"];
    private readonly string? _refreshTokenTime = AppSettingConfig.Configuration["JwtSettings:RefreshTokenTime"];
    
    private readonly IMongoCollection<RefreshTokenModel> _tokenCollection = mongoDbContext
        .GetCollection<RefreshTokenModel>(AppSettingConfig.Configuration["MongoDBSettings:TokenCollection"]!);
    private readonly IMongoCollection<UserModel> _userCollection = mongoDbContext
        .GetCollection<UserModel>(AppSettingConfig.Configuration["MongoDBSettings:UserCollection"]!);
    
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public async Task<string> GenerateAccessToken(string userId, CancellationToken cancellationToken)
    {
        var userModel = await _userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("user-id", userModel.Id),
            new("username", userModel.Username),
            new("email", userModel.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in userModel.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_accessTokenTime)),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshToken(string userId, string deviceId, CancellationToken cancellationToken)
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        
        var refreshToken = new RefreshTokenModel
        {
            UserId = userId,
            RefreshToken = Convert.ToBase64String(randomNumber),
            Expiration = DateTime.UtcNow.AddDays(Convert.ToDouble(_refreshTokenTime)),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            IsRevoked = false,
            Ip = currentUserService.GetIpAddress(),
            DeviceId = deviceId
        };

        try
        {
            await _tokenCollection.InsertOneAsync(refreshToken, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error("[Tokenservice.GenerateRefreshToken] Refresh Token insert failed");
            throw new AppException("Refresh token insert failed");
        }
        
        return refreshToken.RefreshToken;
    }

    public async Task<TokensModel> RenewTokens(string refreshToken, string deviceId, CancellationToken cancellationToken)
    {
        var filter = Builders<RefreshTokenModel>.Filter.And(
            Builders<RefreshTokenModel>.Filter.Eq(x => x.RefreshToken, refreshToken),
            Builders<RefreshTokenModel>.Filter.Eq(x => x.IsUsed, false),
            Builders<RefreshTokenModel>.Filter.Eq(x => x.IsRevoked, false),
            Builders<RefreshTokenModel>.Filter.Eq(x => x.DeviceId, deviceId)
        );
        var activeRefreshToken = await _tokenCollection.Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        if (activeRefreshToken is null || activeRefreshToken.Expiration < DateTime.UtcNow)
        {
            Logger.Error($"[TokenService.RenewTokens] RefreshToken is expired or not found");
            throw new AppException("RefreshToken is expired or not found", 404);
        }

        var update = Builders<RefreshTokenModel>.Update
            .Set(z => z.IsUsed, true);
        try
        {
            await _tokenCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            Logger.Error($"[TokenService.RenewTokens] Error updating refresh token", e);
            throw new AppException("Error updating refresh token", 500);
        }

        var newTokens = new TokensModel
        {
            RefreshToken = await GenerateRefreshToken(activeRefreshToken.UserId, deviceId, cancellationToken),
            AccessToken = await GenerateAccessToken(activeRefreshToken.UserId, cancellationToken),
        };
        return newTokens;
    }

    public async Task<bool> RevokeAllTokens(string userId, string deviceId, CancellationToken cancellationToken)
    {
        var filter = Builders<RefreshTokenModel>.Filter.And(
            Builders<RefreshTokenModel>.Filter.Eq(x => x.UserId, userId),
            Builders<RefreshTokenModel>.Filter.Eq(x => x.DeviceId, deviceId),
            Builders<RefreshTokenModel>.Filter.Eq(x => x.IsRevoked, false)
        );

        var update = Builders<RefreshTokenModel>.Update
            .Set(x => x.IsRevoked, true);

        var result = await _tokenCollection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

}