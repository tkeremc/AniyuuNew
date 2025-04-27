using Aniyuu.Interfaces.UserInterfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers;

[ApiController]
[Route("status")]
[EnableCors("CorsApi")]
public class StatusController(IUserService userService) : ControllerBase
{
    [HttpGet("alive")]
    public async Task<IActionResult> Alive(CancellationToken cancellationToken)
    {
        return Ok("Service is alive!");
    }

    [HttpGet("username-available")]
    public async Task<IActionResult> UsernameAvailable(string username, CancellationToken cancellationToken)
    {
        var result = await userService.CheckUsername(username, cancellationToken); //true un var kotu // false un yok iyi
        return result ? StatusCode(StatusCodes.Status400BadRequest, "username is already registered.") : StatusCode(StatusCodes.Status200OK, "username is available.");
    }
}