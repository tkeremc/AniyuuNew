using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Models.UserModels;
using Aniyuu.ViewModels.UserViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers;

[ApiController]
[Route("auth")]
[EnableCors("CorsApi")]
public class AuthController(IAuthService authService, IMapper mapper, ICurrentUserService currentUserService, ITokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<bool>> Register(UserCreateViewModel userCreateViewModel,
        CancellationToken cancellationToken)
    {
        var userModel = mapper.Map<UserModel>(userCreateViewModel);
        var registerStatus = await authService.Register(userModel, cancellationToken);
        return Ok(registerStatus);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokensViewModel>> Login(UserLoginViewModel userLoginViewModel,
        CancellationToken cancellationToken)
    {
        var userTokens = await authService.Login(userLoginViewModel.Email, userLoginViewModel.Password, cancellationToken);
        var tokensViewModel = mapper.Map<TokensViewModel>(userTokens);
        return Ok(tokensViewModel);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<bool>> Logout(CancellationToken cancellationToken)
    {
        var result = await authService.Logout(cancellationToken);
        return Ok(result);
    }
    
    [HttpGet("refresh-token")]
    public async Task<ActionResult<TokensViewModel>> RefreshToken(string refreshToken,
        CancellationToken cancellationToken)
    {
        var newTokens = await tokenService.RenewTokens(refreshToken, currentUserService.GetDeviceId(), cancellationToken);
        var tokensViewModel = mapper.Map<TokensViewModel>(newTokens);
        return Ok(tokensViewModel);
    }
}