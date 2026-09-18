using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SC_GameServer.GameEngine;
using SC_GameServer.Hubs;
using SC_GameServer.Models;
using SC_GameServer.Services;

namespace SC_GameServer.Messaging;

public class GameCreatedConsumer : BackgroundService
{
    private readonly IConnection          _connection;
    private readonly IGameStateManager    _gameStateManager;
    private readonly IGameEngine          _gameEngine;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameCreatedConsumer> _logger;

    private IChannel? _channel;

    public GameCreatedConsumer(
        IConnection connection,
        IGameStateManager gameStateManager,
        IGameEngine gameEngine,
        IHubContext<GameHub> hubContext,
        ILogger<GameCreatedConsumer> logger)
    {
        _connection       = connection;
        _gameStateManager = gameStateManager;
        _gameEngine       = gameEngine;
        _hubContext       = hubContext;
        _logger           = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(QueueNames.GameCreated, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var msg  = JsonSerializer.Deserialize<GameCreatedMessage>(json);

                if (msg is null)
                {
                    _logger.LogWarning("Received unparseable {Queue} message", QueueNames.GameCreated);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                var creation = _gameEngine.CreateGame(msg.GameId, msg.GameSettings, msg.Players);

                _gameStateManager.AddGame(new GameInstance
                {
                    GameId     = msg.GameId,
                    Settings   = msg.GameSettings,
                    Players    = msg.Players,
                    BoardState = creation.BoardState
                });

                _logger.LogInformation("Game {GameId} created with {PlayerCount} players", msg.GameId, msg.Players.Count);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process {Queue} message", QueueNames.GameCreated);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(QueueNames.GameCreated, autoAck: false, consumer, cancellationToken: stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
