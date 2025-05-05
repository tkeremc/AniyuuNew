using System.Text;
using System.Text.Json;
using Aniyuu.Interfaces;
using Aniyuu.Utils;
using RabbitMQ.Client;

namespace Aniyuu.Services.MessageBroker;

public class MessagePublisherService(MessageConnectionProvider provider) : IMessagePublisherService
{

    public async Task PublishAsync<T>(T message, string exchangeName, string routingKey)
    {
        var retryCount = routingKey == "notification" ? 5 : 1;
        
        using var channel = provider.CreateChannel(); 

        var messageJson = JsonSerializer.Serialize(message);
        var hash = ComputeHash(messageJson);

        var payload = new
        {
            Message = message,
            Hash = hash
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var body = Encoding.UTF8.GetBytes(payloadJson);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Headers = new Dictionary<string, object>
        {
            {"x-retry-count", retryCount}
        };

        channel.BasicPublish(
            exchange: exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body
        );

        await Task.CompletedTask; 
    }

    private string ComputeHash(string message)
    {
        var key = Encoding.UTF8.GetBytes(AppSettingConfig.Configuration["MessageBroker:SecretKey"]!);
        using var hmac = new System.Security.Cryptography.HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }
}