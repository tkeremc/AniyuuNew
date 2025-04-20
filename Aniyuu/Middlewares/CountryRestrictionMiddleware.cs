using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aniyuu.Middlewares;

public class CountryRestrictionMiddleware(
    RequestDelegate next,
    ILogger<CountryRestrictionMiddleware> logger,
    IHostEnvironment env)
{
    private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public async Task InvokeAsync(HttpContext context)
    {
        string ip = GetClientIpAddress(context);

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

        if (ip == "127.0.0.1")
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
        if (_cache.TryGetValue(ip, out bool isTurkeyIp))
        {
            if (!isTurkeyIp)
            {
                logger.LogWarning($"Cached - Access denied for IP: {ip}.");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("No access.");
                return;
            }

            await next(context);
            return;
        }

        // IP adresinin Türkiye olup olmadığını kontrol et
        isTurkeyIp = await IsIpFromTurkey(ip);

        // Eğer API başarısız olursa varsayılan olarak erişimi reddet
        var cacheTime = isTurkeyIp ? TimeSpan.FromHours(6) : TimeSpan.FromHours(1);
        _cache.Set(ip, isTurkeyIp, cacheTime);

        if (!isTurkeyIp)
        {
            logger.LogWarning($"Checked - Access denied for IP: {ip}.");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("No access.");
            return;
        }

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

    private async Task<bool> IsIpFromTurkey(string ip)
    {
        try
        {
            var isTurkeyIp = await CheckIpWithPrimaryApi(ip);
            if (isTurkeyIp.HasValue)
                return isTurkeyIp.Value;

            logger.LogWarning($"Primary API failed, trying backup API for IP: {ip}");
            isTurkeyIp = await CheckIpWithBackupApi(ip);

            return isTurkeyIp ?? false;
        }
        catch (Exception ex)
        {
            logger.LogError($"IP check failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool?> CheckIpWithPrimaryApi(string ip)
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

                return ipInfo.CountryCode?.ToUpper() == "TR";
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Primary API error: {ex.Message}");
        }

        return null;
    }

    private async Task<bool?> CheckIpWithBackupApi(string ip)
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

                return ipInfo.CountryCode?.ToUpper() == "TR";
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Backup API error: {ex.Message}");
        }

        return null;
    }

    private class IpApiResponse
    {
        public string? Status { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public string? Region { get; set; }
        public string? RegionName { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public double? Lat { get; set; }
        public double? Lon { get; set; }
        public string? Timezone { get; set; }
        public string? Isp { get; set; }
        public string? Org { get; set; }
        public string? As { get; set; }
        public string? Query { get; set; }
    }
}