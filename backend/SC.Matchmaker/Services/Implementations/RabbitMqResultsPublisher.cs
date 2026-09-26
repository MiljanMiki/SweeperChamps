using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using SC.Matchmaker.Services.Interfaces;
using SC.Messaging;
using SC.Messaging.Matchmaker;
using System.Text;
using System.Text.Json;

namespace SC_Backend.Matchmaker.Services;

public class RabbitMqResultsPublisher : IMatchFoundPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMqResultsPublisher(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public void Publish(MatchFoundEvent matchEvent)
    {
        var json = JsonSerializer.Serialize(matchEvent);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent };

        _channel.BasicPublishAsync(
            exchange: RabbitMqConstants.MatchmakingExchange,
            routingKey: "match.found",
            mandatory: false,
            basicProperties: props,
            body: body
        ).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (_channel.IsOpen) _channel.CloseAsync().GetAwaiter().GetResult();
        if (_connection.IsOpen) _connection.CloseAsync().GetAwaiter().GetResult();
    }
}