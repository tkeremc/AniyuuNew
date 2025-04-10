using Aniyuu.Models;
using Aniyuu.Models.UserModels;

namespace Aniyuu.Interfaces.UserInterfaces;

public interface ITokenService
{
    Task<string> GenerateAccessToken(UserModel userModel,  CancellationToken cancellationToken);
    Task<RefreshTokenModel> GenerateRefreshToken(string userId, string deviceId, CancellationToken cancellationToken);
    Task<TokensModel> RenewTokens(string refreshToken, CancellationToken cancellationToken);
    Task<bool> RevokeAllTokens(string userId, string deviceId, CancellationToken cancellationToken);
}