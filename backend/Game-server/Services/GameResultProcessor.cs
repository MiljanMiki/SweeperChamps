using Microsoft.AspNetCore.SignalR;
using SC_GameServer.GameEngine;
using SC_GameServer.Hubs;
using SC_GameServer.Messaging;
using SC_GameServer.Models;

namespace SC_GameServer.Services;

/// <summary>
/// Single place that turns an engine MoveResult into: a broadcast to the
/// game's SignalR group, a persistence event on RabbitMQ, and (for TimeRush)
/// scheduling the next player's move timer. Used by GameHub.MakeMove and by
/// the turn-timeout callback itself, so both paths behave identically.
/// </summary>
public interface IGameResultProcessor
{
    Task ProcessAsync(GameInstance game, int playerId, MoveResult result);

    /// <summary>
    /// Starts (or restarts) the countdown for the given player's turn.
    /// Any previously pending timer for this game is cancelled first.
    /// </summary>
    void ScheduleTurnTimeout(GameInstance game, int playerId, int seconds);
}

public class GameResultProcessor : IGameResultProcessor
{
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IRabbitMqPublisher _publisher;
    private readonly IGameStateManager _gameStateManager;
    private readonly IGameEngine _gameEngine;
    private readonly ILogger<GameResultProcessor> _logger;

    public GameResultProcessor(
        IHubContext<GameHub> hubContext,
        IRabbitMqPublisher publisher,
        IGameStateManager gameStateManager,
        IGameEngine gameEngine,
        ILogger<GameResultProcessor> logger)
    {
        _hubContext = hubContext;
        _publisher = publisher;
        _gameStateManager = gameStateManager;
        _gameEngine = gameEngine;
        _logger = logger;
    }

    public async Task ProcessAsync(GameInstance game, int playerId, MoveResult result)
    {
        if (!result.IsValid)
        {
            // Invalid results are surfaced to just the calling player by
            // GameHub.MakeMove itself - nothing here to broadcast or persist.
            return;
        }

        await _hubContext.Clients.Group(game.GroupName).SendAsync("MoveMade", new
        {
            playerId,
            payload = result.BroadcastPayload
        });

        await _publisher.PublishMoveMadeAsync(new MoveMadeMessage
        {
            GameId = game.GameId,
            PlayerId = playerId,
            Timestamp = DateTime.UtcNow,
            MoveLogJson = result.MoveLogJson
        });

        if (result.GameOver)
        {
            game.IsFinished = true;
            game.TurnTimerCts?.Cancel();

            await _hubContext.Clients.Group(game.GroupName).SendAsync("GameOver", result.FinalResults);

            await _publisher.PublishGameFinishedAsync(new GameFinishedMessage
            {
                GameId = game.GameId,
                EndTime = DateTime.UtcNow,
                Status = "Finished",
                Results = result.FinalResults ?? new List<PlayerResultDto>()
            });

            _gameStateManager.RemoveGame(game.GameId);
            return;
        }

        if (result.NextPlayerId is int nextPlayerId && result.NextMoveDeadlineSeconds is int deadline)
        {
            ScheduleTurnTimeout(game, nextPlayerId, deadline);
        }
    }

    public void ScheduleTurnTimeout(GameInstance game, int playerId, int seconds)
    {
        game.TurnTimerCts?.Cancel();
        var cts = new CancellationTokenSource();
        game.TurnTimerCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), cts.Token);
            }
            catch (TaskCanceledException)
            {
                return; // a move arrived in time - nothing to do
            }

            try
            {
                var result = _gameEngine.ApplyTimeout(game.GameId, playerId);
                await ProcessAsync(game, playerId, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing turn timeout for game {GameId}, player {PlayerId}", game.GameId, playerId);
            }
        }, cts.Token);
    }
}
