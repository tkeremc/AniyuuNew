using Aniyuu.Interfaces.AdminServices;
using Aniyuu.ViewModels.UserViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers.AdminController;

[ApiController]
[Route("admin/user")]
[EnableCors("CorsApi")]
[Authorize(Roles = "admin")]
public class AdminUserController(IAdminUserService adminUserService,
    IMapper mapper) : ControllerBase
{
    [HttpGet("get-all")]
    public async Task<ActionResult<List<UserViewModel>>> GetAll(int page, int count, CancellationToken cancellationToken)
    {
        var users = await adminUserService.GetAllUsers(page, count, cancellationToken);
        var userViewModel = mapper.Map<List<UserViewModel>>(users);
        return userViewModel;
    }

    [HttpGet("get")]
    public async Task<ActionResult<UserViewModel>> Get(string username, CancellationToken cancellationToken)
    {
        var user = await adminUserService.GetUser(username, cancellationToken);
        var userViewModel = mapper.Map<UserViewModel>(user);
        return userViewModel;
    }

    [HttpPut("set-user-admin")]
    public async Task<ActionResult<bool>> SetUserAsAdmin(string username, CancellationToken cancellationToken)
    {
        return await adminUserService.SetUserAsAdmin(username, cancellationToken);
    }

    [HttpDelete("delete-user")]
    public async Task<ActionResult<bool>> Delete(string username, CancellationToken cancellationToken)
    {
        return await adminUserService.DeleteUser(username, cancellationToken);
    }
}