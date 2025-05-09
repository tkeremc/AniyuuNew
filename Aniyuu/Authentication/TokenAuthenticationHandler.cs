using System.IdentityModel.Tokens.Jwt;
using UAParser;

namespace Aniyuu.Authentication;

public class TokenAuthenticationHandler(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        string deviceId = context.Request.Headers["device-id"];
        context.Request.Headers["X-Device-Id"] = deviceId;
        
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() 
                        ?? context.Connection.RemoteIpAddress?.ToString();
        context.Request.Headers["X-IP-Address"] = ipAddress;

        await GetClientInfo(context);
        
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            AttachUserToContext(context, token);
        }

        await next(context);
    }

    private void AttachUserToContext(HttpContext context, string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            
            var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == "user-id")?.Value;
            var username = jwtToken.Claims.FirstOrDefault(x => x.Type == "username")?.Value;
            var email = jwtToken.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
            var roles = jwtToken.Claims.Where(x => x.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(x => x.Value).ToList();

            // Header'a claim bilgilerini ekleyelim
            context.Request.Headers["X-User-Id"] = userId;
            context.Request.Headers["X-Username"] = username;
            context.Request.Headers["X-Email"] = email;
            context.Request.Headers["X-Roles"] = string.Join(",", roles);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private Task GetClientInfo(HttpContext context)
    {
        var uaString = context.Request.Headers.UserAgent;
        var uaParser = Parser.GetDefault();
        var clientInfo = uaParser.Parse(uaString);
        context.Request.Headers["X-Browser-Info"] =
            $"{clientInfo.UA.Family} {clientInfo.UA.Major}.{clientInfo.UA.Minor}";
        context.Request.Headers["X-Client-Info"] = $"{clientInfo.OS.Family} {clientInfo.OS.Major}";
        return Task.CompletedTask;
    }
}