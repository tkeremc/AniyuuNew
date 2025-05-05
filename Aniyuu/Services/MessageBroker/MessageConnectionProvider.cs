using RabbitMQ.Client;

namespace Aniyuu.Services.MessageBroker;

public class MessageConnectionProvider : IDisposable
{
    private readonly IConnection _connection;

    public MessageConnectionProvider(ConnectionFactory factory)
    {
        // Uygulama ilk başladığında bir kere Connection açılır
        _connection = factory.CreateConnection();
    }

    public IModel CreateChannel()
    {
        // Her işlem için yeni bir Channel açılır
        return _connection.CreateModel();
    }

    public void Dispose()
    {
        // Uygulama kapanınca Connection kapatılır
        _connection?.Dispose();
    }
}