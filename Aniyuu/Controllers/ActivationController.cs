using Aniyuu.Interfaces.UserInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers;

[ApiController]
[Route("activation")]
[EnableCors("CorsApi")]
public class ActivationController(IActivationService activationService) : ControllerBase
{
    [HttpPut("activate-user")]
    public async Task<ActionResult<bool>> ActivateUser(int code, CancellationToken cancellationToken)
    {
        var result = await activationService
            .ActivateUser(code, cancellationToken);
        return result ? Ok(result) : BadRequest(result);
    }
    
    [HttpGet("resend-activation")]
    public async Task<ActionResult<bool>> ResendActivation(string email, CancellationToken cancellationToken)
    {
        var result = await activationService.ResendActivationCode(email, cancellationToken);
        return Ok(result);
    }
}