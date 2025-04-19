using Aniyuu.Models.UserModels;

namespace Aniyuu.Interfaces.UserInterfaces;

public interface IUserService
{
    Task<UserModel> Get(CancellationToken cancellationToken);
    Task<string> GetEmail(string username, CancellationToken cancellationToken);
    Task<UserModel> Update(UserModel updatedUserModel, CancellationToken cancellationToken);
    Task<bool> Delete(CancellationToken cancellationToken);
    Task<bool> ChangePassword(string oldPassword, string newPassword, CancellationToken cancellationToken);
}