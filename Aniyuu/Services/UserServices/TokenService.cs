using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aniyuu.DbContext;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models;
using Aniyuu.Models.UserModels;
using Aniyuu.Utils;
using Microsoft.IdentityModel.Tokens;

namespace Aniyuu.Services.UserServices;

public class TokenService(IMongoDbContext mongoDbContext,
    ICurrentUserService currentUserService) :  ITokenService
{
    public async Task<string> GenerateAccessToken(UserModel userModel, CancellationToken cancellationToken)
    {
        var secretKey = AppSettingConfig.Configuration["JwtSettings:SecretKey"];
        var audience = AppSettingConfig.Configuration["JwtSettings:Audience"];
        var issuer = AppSettingConfig.Configuration["JwtSettings:Issuer"];
        var accessTokenTime = AppSettingConfig.Configuration["JwtSettings:AccessTokenTime"];
        var refreshTokenTime = AppSettingConfig.Configuration["JwtSettings:RefreshTokenTime"];

        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
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
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(accessTokenTime)),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RefreshTokenModel> GenerateRefreshToken(string userId, string deviceId, CancellationToken cancellationToken)
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        
        var refreshToken = new RefreshTokenModel
        {
            UserId = userId,
            RefreshToken = Convert.ToBase64String(randomNumber),
            Expiration = DateTime.UtcNow.AddDays(7), // 7 gün geçerli olacak
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            IsRevoked = false,
            Ip = currentUserService.GetIpAddress(),
            DeviceId = deviceId // ✅ Cihaz ID ekleniyor
        };
        
        return refreshToken;
    }

    public Task<TokensModel> RenewTokens(string refreshToken, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeAllTokens(string userId, string deviceId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}