using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace SC_GameServer.Messaging;

public interface IRabbitMqPublisher
{
    Task PublishMoveMadeAsync(MoveMadeMessage message);
    Task PublishGameFinishedAsync(GameFinishedMessage message);
}

public class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private readonly IConnection    _connection;
    private IChannel?               _channel;
    private readonly SemaphoreSlim  _initLock = new(1, 1);

    public RabbitMqPublisher(IConnection connection)
    {
        _connection = connection;
    }

    private async Task<IChannel> GetChannelAsync()
    {
        if (_channel is not null) return _channel;

        await _initLock.WaitAsync();
        try
        {
            if (_channel is null)
            {
                _channel = await _connection.CreateChannelAsync();
                await _channel.QueueDeclareAsync(QueueNames.GameMoves,    durable: true, exclusive: false, autoDelete: false);
                await _channel.QueueDeclareAsync(QueueNames.GameFinished, durable: true, exclusive: false, autoDelete: false);
            }
        }
        finally { _initLock.Release(); }

        return _channel;
    }

    public Task PublishMoveMadeAsync(MoveMadeMessage message)       => PublishAsync(QueueNames.GameMoves,    message);
    public Task PublishGameFinishedAsync(GameFinishedMessage message) => PublishAsync(QueueNames.GameFinished, message);

    private async Task PublishAsync<T>(string queue, T message)
    {
        var channel = await GetChannelAsync();
        var body    = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props   = new BasicProperties { Persistent = true };
        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queue, mandatory: false, basicProperties: props, body: body);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
