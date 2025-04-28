namespace Aniyuu.Interfaces.MessageBrokerInterfaces;

public interface IMessagePublisherService
{
    Task PublishAsync<T>(T message, string exchangeName, string routingKey);
}