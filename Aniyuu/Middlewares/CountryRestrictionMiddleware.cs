using System.Net;
using System.Text.Json;
using Aniyuu.Exceptions;
using Aniyuu.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Aniyuu.Middlewares;

public class CountryRestrictionMiddleware(
    RequestDelegate next,
    ILogger<CountryRestrictionMiddleware> logger,
    IHostEnvironment env)
{
    private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = GetClientIpAddress(context);

        if (env.IsDevelopment())
        {
            logger.LogInformation("Development mode. Middleware skipped.");
            await next(context);
            return;
        }

        if (string.IsNullOrEmpty(ip))
        {
            logger.LogWarning("Cannot determine client IP.");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("No access.");
            return;
        }

        if (ip is "127.0.0.1" or "::1")
        {
            logger.LogInformation("Local request, skipped.");
            await next(context);
            return;
        }

        // Sadece IPv4 adreslerini kontrol et
        if (!IsValidIPv4(ip))
        {
            logger.LogWarning($"Invalid IPv4 format detected: {ip}");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid IPv4.");
            return;
        }

        // Eğer cache'de varsa, direkt sonucu döndür
        if (_cache.TryGetValue(ip, out IpCheckResult cachedResult))
        {
            if (!cachedResult.IsFromTR)
            {
                logger.LogWarning($"Cached - Access denied for IP: {ip}.");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("No access.");
                return;
            }

            context.Request.Headers["X-User-Address"] = cachedResult.Location ?? "Unknown";
            await next(context);
            return;
        }

        var result = await IsIpFromTurkey(ip, context);
        _cache.Set(ip, result, result.IsFromTR ? TimeSpan.FromHours(6) : TimeSpan.FromHours(1));

        if (!result.IsFromTR)
        {
            logger.LogWarning($"Checked - Access denied for IP: {ip}.");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("No access.");
            return;
        }

        context.Request.Headers["X-User-Address"] = result.Location ?? "Unknown";
        await next(context);

    }

    private string GetClientIpAddress(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var headerValue))
        {
            var ips = headerValue.ToString().Split(',').Select(ip => ip.Trim()).ToArray();
            if (ips.Length > 0)
            {
                return ips[0]; // İlk IP gerçek istemcinin IP'sidir
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }

    private bool IsValidIPv4(string ip)
    {
        return IPAddress.TryParse(ip, out var parsedIp) && parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    }

    private async Task<IpCheckResult> IsIpFromTurkey(string ip, HttpContext context)
    {
        try
        {
            var isTurkeyIp = await CheckIpWithPrimaryApi(ip);
            if (isTurkeyIp == null)
            {
                logger.LogWarning($"Primary API failed, trying backup API for IP: {ip}");
                isTurkeyIp = await CheckIpWithBackupApi(ip);
            }

            if (isTurkeyIp.CountryCode?.ToUpper() != "TR")
            {
                throw new AppException("Invalid country code.");
            }
            context.Request.Headers["X-User-Address"] = $"{isTurkeyIp.Country}, {isTurkeyIp.City}";
            return new IpCheckResult
            {
                IsFromTR = true,
                Location = $"{isTurkeyIp.Country}, {isTurkeyIp.City}"
            };
        }
        catch (Exception ex)
        {
            logger.LogError($"IP check failed: {ex.Message}");
            throw new AppException("Ip check failed",500);
        }
    }

    private async Task<IpApiResponse?> CheckIpWithPrimaryApi(string ip)
    {
        try
        {
            using var httpClient = new HttpClient();
            string url = $"http://ip-api.com/json/{ip}";
            var response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var ipInfo = JsonSerializer.Deserialize<IpApiResponse>(json, options);

                if (ipInfo == null || ipInfo.Status != "success")
                {
                    logger.LogWarning($"Primary API returned failure for IP: {ip}. Response: {json}");
                    return null;
                }

                return ipInfo;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Primary API error: {ex.Message}");
        }

        return null;
    }

    private async Task<IpApiResponse?> CheckIpWithBackupApi(string ip)
    {
        try
        {
            using var httpClient = new HttpClient();
            string url = $"https://ipapi.co/{ip}/json/";
            var response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var ipInfo = JsonSerializer.Deserialize<IpApiResponse>(json, options);
                
                if (ipInfo == null)
                {
                    logger.LogWarning($"Backup API returned failure for IP: {ip}. Response: {json}");
                    return null;
                }

                return ipInfo;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Backup API error: {ex.Message}");
        }

        return null;
    }
    private class IpCheckResult
    {
        public bool IsFromTR { get; set; }
        public string? Location { get; set; }
    }
}