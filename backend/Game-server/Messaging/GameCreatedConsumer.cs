using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SC_GameServer.GameEngine;
using SC_GameServer.Models;
using SC_GameServer.Services;

namespace SC_GameServer.Messaging;

/// <summary>
/// Listens on "game.created" (published by the web API once a match + DB
/// rows exist). Builds the in-memory GameInstance, initializes the engine's
/// board, and - for TimeRush - starts the first turn's timer. Players then
/// connect and call Hub.JoinGame(gameId).
/// Written against RabbitMQ.Client 7.x's async API (IChannel, AsyncEventingBasicConsumer).
/// </summary>
public class GameCreatedConsumer : BackgroundService
{
    private const string GameCreatedQueue = "game.created";

    private readonly IConnection _connection;
    private readonly IGameStateManager _gameStateManager;
    private readonly IGameEngine _gameEngine;
    private readonly IGameResultProcessor _resultProcessor;
    private readonly ILogger<GameCreatedConsumer> _logger;

    private IChannel? _channel;

    public GameCreatedConsumer(
        IConnection connection,
        IGameStateManager gameStateManager,
        IGameEngine gameEngine,
        IGameResultProcessor resultProcessor,
        ILogger<GameCreatedConsumer> logger)
    {
        _connection = connection;
        _gameStateManager = gameStateManager;
        _gameEngine = gameEngine;
        _resultProcessor = resultProcessor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(GameCreatedQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var msg = JsonSerializer.Deserialize<GameCreatedMessage>(json);
                if (msg is null)
                {
                    _logger.LogWarning("Received unparseable game.created message");
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                var creation = _gameEngine.CreateGame(msg.GameId, msg.GameSettings, msg.Players);

                var instance = new GameInstance
                {
                    GameId = msg.GameId,
                    Settings = msg.GameSettings,
                    Players = msg.Players,
                    BoardState = creation.BoardState
                };

                _gameStateManager.AddGame(instance);
                _logger.LogInformation("Game {GameId} created with {PlayerCount} players", msg.GameId, msg.Players.Count);

                // TimeRush: start the first player's clock now, before anyone
                // has necessarily connected yet - matches "auto-lose on
                // timeout" even if a player never joins in time.
                if (creation.FirstTurnPlayerId is int firstPlayerId && creation.MoveDeadlineSeconds is int deadline)
                {
                    _resultProcessor.ScheduleTurnTimeout(instance, firstPlayerId, deadline);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process game.created message");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(GameCreatedQueue, autoAck: false, consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}