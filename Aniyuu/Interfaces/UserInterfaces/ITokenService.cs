using Aniyuu.Models;
using Aniyuu.Models.UserModels;

namespace Aniyuu.Interfaces.UserInterfaces;

public interface ITokenService
{
    Task<string> GenerateAccessToken(string userId, CancellationToken cancellationToken);
    Task<string> GenerateRefreshToken(string userId, string deviceId, CancellationToken cancellationToken);

    Task<TokensModel> RenewTokens(string userId, string refreshToken, string deviceId,
        CancellationToken cancellationToken);
    Task<bool> RevokeAllTokens(string userId, string deviceId, CancellationToken cancellationToken);
}