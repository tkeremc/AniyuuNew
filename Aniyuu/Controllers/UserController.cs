using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models.UserModels;
using Aniyuu.ViewModels.UserViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers;

[ApiController]
[Route("user")]
[EnableCors("CorsApi")]
[Authorize]
public class UserController(IUserService userService, IMapper mapper) : ControllerBase
{
    [HttpGet("get")]
    public async Task<ActionResult<UserViewModel>> Get(CancellationToken cancellationToken)
    {
        var userModel = await userService.Get(cancellationToken);
        var userViewModel = mapper.Map<UserViewModel>(userModel);
        return Ok(userViewModel);
    }

    [HttpGet("get-email")]
    public async Task<ActionResult<string>> GetEmail(string username, CancellationToken cancellationToken)
    {
        var email = await userService.GetEmail(username, cancellationToken);
        return Ok(email);
    }

    [HttpPut("update")]
    public async Task<ActionResult<UserViewModel>> Update(UserUpdateViewModel userUpdateViewModel,
        CancellationToken cancellationToken)
    {
        var userModel = mapper.Map<UserModel>(userUpdateViewModel);
        var updatedUserModel = await userService.Update(userModel, cancellationToken);
        var userViewModel = mapper.Map<UserViewModel>(updatedUserModel);
        return StatusCode(StatusCodes.Status200OK, userViewModel);
    }

    [HttpDelete("delete")]
    public async Task<ActionResult<bool>> Delete(CancellationToken cancellationToken)
    {
        var isUserDeleted = await userService.Delete(cancellationToken);
        return Ok(isUserDeleted);
    }

    [HttpPut("change-password")]
    public async Task<ActionResult<bool>> ChangePassword(UserPasswordUpdateViewModel userPasswordUpdateViewModel,
        CancellationToken cancellationToken)
    {
        var user = await userService.ChangePassword(userPasswordUpdateViewModel.CurrentPassword,
            userPasswordUpdateViewModel.NewPassword, cancellationToken);
        return Ok(user);
    }
}