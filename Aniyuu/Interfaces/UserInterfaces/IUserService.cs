using Aniyuu.Models.UserModels;

namespace Aniyuu.Interfaces.UserInterfaces;

public interface IUserService
{
    Task<UserModel> Get(CancellationToken cancellationToken);
    Task<string> GetEmail(string username, CancellationToken cancellationToken);
    Task<bool> CheckUsername(string username, CancellationToken cancellationToken);
    Task<UserModel> Update(UserModel updatedUserModel, CancellationToken cancellationToken,
        string updatedBy = "system");
    Task<string> UpdateAvatar(IFormFile file, CancellationToken cancellationToken);
    Task<bool> Delete(CancellationToken cancellationToken);
    Task<bool> ChangePassword(string oldPassword, string newPassword, CancellationToken cancellationToken);
}