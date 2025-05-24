using Aniyuu.Models.UserModels;
using Aniyuu.ViewModels.UserViewModels;

namespace Aniyuu.Interfaces.AdminServices;

public interface IAdminUserService
{
    Task<List<UserModel>> GetAllUsers(int page, int count, CancellationToken cancellationToken);
    Task<UserModel> GetUser(string username, CancellationToken cancellationToken);
    Task<bool> SetUserAsAdmin(string username, CancellationToken cancellationToken);
    Task<bool> DeleteUser(string username, CancellationToken cancellationToken);
}