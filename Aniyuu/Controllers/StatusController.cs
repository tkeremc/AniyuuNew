using Aniyuu.Interfaces.MessageBrokerInterfaces;
using Aniyuu.Interfaces.UserInterfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers;

[ApiController]
[Route("status")]
[EnableCors("CorsApi")]
public class StatusController(IUserService userService,
    IMessagePublisherService messagePublisherService) : ControllerBase
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

    [HttpGet("test-message")]
    public async Task<ActionResult<string>> TestMessage(string message, string exchangeName, string queue, CancellationToken cancellationToken)
    {
        messagePublisherService.PublishAsync(message, exchangeName,queue);
        return Ok($"Test Message sent to {queue}. Message: {message}.");
    }
}