using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace SC_GameServer.Messaging;

public interface IRabbitMqPublisher
{
    void PublishMoveMade(MoveMadeMessage message);
    void PublishGameFinished(GameFinishedMessage message);
}

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private const string MovesQueue = "game.moves";
    private const string GameFinishedQueue = "game.finished";

    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqPublisher(IConnection connection)
    {
        _connection = connection;
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(MovesQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(GameFinishedQueue, durable: true, exclusive: false, autoDelete: false);
    }

    public void PublishMoveMade(MoveMadeMessage message) => Publish(MovesQueue, message);

    public void PublishGameFinished(GameFinishedMessage message) => Publish(GameFinishedQueue, message);

    private void Publish<T>(string queue, T message)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;

        _channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: props, body: body);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
