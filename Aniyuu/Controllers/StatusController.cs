using Aniyuu.Helpers;
using Aniyuu.Interfaces;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.ViewModels;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Aniyuu.Controllers;

[ApiController]
[Route("status")]
[EnableCors("CorsApi")]
public class StatusController(IUserService userService,
    IMessagePublisherService messagePublisherService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("alive")]
    public async Task<IActionResult> Alive(CancellationToken cancellationToken)
    {
        return Ok("Service is alive!");
    }

    [HttpGet("is-username-available")]
    public async Task<IActionResult> UsernameAvailable(string username, CancellationToken cancellationToken)
    {
        var result = await userService.CheckUsername(username, cancellationToken); 
        return result ? StatusCode(StatusCodes.Status400BadRequest, "username is already registered.") : StatusCode(StatusCodes.Status200OK, "username is available.");
    }
    [HttpGet("is-email-available")]
    public async Task<IActionResult> EmailAvailable(string email, CancellationToken cancellationToken)
    {
        var result = await userService.CheckEmail(email, cancellationToken); 
        return result ? StatusCode(StatusCodes.Status400BadRequest, "email is already registered.") : StatusCode(StatusCodes.Status200OK, "email is available.");
    }
    
    [HttpGet("test-message-mailed")]
    public async Task<IActionResult> TestMessageMailed(string to, string exchangeName, string routingKey,
        CancellationToken cancellationToken)
    {
        var message = new EmailMessageViewModel()
        {
            To = to,
            Subject = "Test",
            TemplateName = "WelcomeEmail",
            UsedPlaceholders = new Dictionary<string, string>()
            {
                { "username", "username" },
                { "email", to },
                { "code", "123456" }
            }
        };
        messagePublisherService.PublishAsync(message, exchangeName, routingKey);
        return Ok($"Test Message sent to {routingKey}. Message: {message}.");
    }
    [HttpGet("get-client-details")]
    public IActionResult GetBrowserDetails()
    {
        var result = new
        {
            Browser = new
            {
                Name = currentUserService.GetBrowserData(),
                Location = currentUserService.GetUserAddress()
            },
            OS = new
            {
                Name = currentUserService.GetOSData()
            }
        };

        return Ok(result);
    }

    [HttpGet("test-translate")]
    public async Task<ActionResult<string>> Translate(string text)
    {
        return await TranslateHelper.Translate(text);
    }
}