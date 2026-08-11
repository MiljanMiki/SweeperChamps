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
    private const string GameCreatedQueue = "game.created";

    private readonly IConnection _connection;
    private readonly IGameStateManager _gameStateManager;
    private readonly IGameEngine _gameEngine;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameCreatedConsumer> _logger;

    public GameCreatedConsumer(
        IConnection connection,
        IGameStateManager gameStateManager,
        IGameEngine gameEngine,
        IHubContext<GameHub> hubContext,
        ILogger<GameCreatedConsumer> logger)
    {
        _connection = connection;
        _gameStateManager = gameStateManager;
        _gameEngine = gameEngine;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = _connection.CreateModel();
        channel.QueueDeclare(GameCreatedQueue, durable: true, exclusive: false, autoDelete: false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var msg = JsonSerializer.Deserialize<GameCreatedMessage>(json);
                if (msg is null)
                {
                    _logger.LogWarning("Received unparseable game.created message");
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                var boardState = _gameEngine.CreateGame(msg.GameId, msg.GameSettings, msg.Players);

                var instance = new GameInstance
                {
                    GameId = msg.GameId,
                    Settings = msg.GameSettings,
                    Players = msg.Players,
                    BoardState = boardState
                };

                _gameStateManager.AddGame(instance);
                _logger.LogInformation("Game {GameId} created with {PlayerCount} players", msg.GameId, msg.Players.Count);

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process game.created message");
                channel.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        };

        channel.BasicConsume(GameCreatedQueue, autoAck: false, consumer);

        stoppingToken.Register(() => channel.Close());
        return Task.CompletedTask;
    }
}
