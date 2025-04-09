using Aniyuu.Models.UserModels;

namespace Aniyuu.Interfaces.UserInterfaces;

public interface IUserService
{
    Task<UserModel> GetUser(string userId, CancellationToken cancellationToken);
    Task<string> GetEmail(string username, CancellationToken cancellationToken);
    Task<UserModel> Update(UserModel userModel, CancellationToken cancellationToken);
    Task<bool> Delete(string userId, CancellationToken cancellationToken);
    Task<bool> ChangePassword(string userId, string oldPassword, string newPassword, string confirmPassword, CancellationToken cancellationToken);
}