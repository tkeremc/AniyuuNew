using Aniyuu.DbContext;
using Aniyuu.Models;
using Aniyuu.Utils;
using MongoDB.Driver;

namespace Aniyuu.Middlewares;

public class RequestLogMiddleware(RequestDelegate next, IMongoDbContext mongoDbContext)
{
    private readonly IMongoCollection<RequestLogModel> _requestLogCollection = mongoDbContext.GetCollection<RequestLogModel>(AppSettingConfig.Configuration["MongoDBSettings:LogsCollection"]!);
    
    public async Task Invoke(HttpContext context)
    {
        var request = context.Request;
        var response = context.Response;

        var log = new RequestLogModel
        {
            CreatedAt = DateTime.UtcNow,
            Method = request.Method,
            Path = request.Path,
            UserId = context.Request.Headers["X-User-Id"].FirstOrDefault(),
            IpAddress = context.Request.Headers["X-Ip-Address"].FirstOrDefault(),
            DeviceId = context.Request.Headers["X-Device-Id"].FirstOrDefault()
        };

        var originalBodyStream = response.Body;
        using var responseBody = new MemoryStream();
        response.Body = responseBody;

        await next(context);

        log.StatusCode = response.StatusCode;

        await _requestLogCollection.InsertOneAsync(log);

        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }
}