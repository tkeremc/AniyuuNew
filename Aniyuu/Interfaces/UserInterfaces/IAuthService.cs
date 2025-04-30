using Aniyuu.Models;
using Aniyuu.Models.UserModels;

namespace Aniyuu.Interfaces.UserInterfaces;

public interface IAuthService
{
    Task<bool> Register(UserModel userModel, CancellationToken cancellationToken);
    Task<TokensModel> Login(string email, string password, CancellationToken cancellationToken);
    Task<bool> Logout(CancellationToken cancellationToken);
    Task<string> SendPasswordRecovery(string email, CancellationToken cancellationToken);
    Task<string> RecoverPassword(string passToken, string newPassword, string newPasswordAgain, CancellationToken cancellationToken);
}