using Microsoft.AspNetCore.SignalR;
using SC_GameServer.GameEngine;
using SC_GameServer.Hubs;
using SC_GameServer.Messaging;
using SC_GameServer.Models;

namespace SC_GameServer.Services;

public class GameResultProcessor
{
    private readonly IGameEngine          _gameEngine;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IRabbitMqPublisher   _publisher;
    private readonly IGameStateManager    _gameStateManager;
    private readonly ILogger<GameResultProcessor> _logger;

    public GameResultProcessor(
        IGameEngine gameEngine,
        IHubContext<GameHub> hubContext,
        IRabbitMqPublisher publisher,
        IGameStateManager gameStateManager,
        ILogger<GameResultProcessor> logger)
    {
        _gameEngine       = gameEngine;
        _hubContext       = hubContext;
        _publisher        = publisher;
        _gameStateManager = gameStateManager;
        _logger           = logger;
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
                return; // player moved in time
            }

            if (cts.Token.IsCancellationRequested) return;

            var result = _gameEngine.ApplyTimeout(game.GameId, playerId);

            await _hubContext.Clients
                .Group(game.GroupName)
                .SendAsync(HubEvents.PlayerTimeout, new { playerId, result.BroadcastPayload });

            await _publisher.PublishMoveMadeAsync(new MoveMadeMessage
            {
                GameId      = game.GameId,
                PlayerId    = playerId,
                Timestamp   = DateTime.UtcNow,
                MoveLogJson = result.MoveLogJson
            });

            if (result.GameOver)
            {
                game.IsFinished = true;

                await _hubContext.Clients
                    .Group(game.GroupName)
                    .SendAsync(HubEvents.GameOver, result.FinalResults);

                await _publisher.PublishGameFinishedAsync(new GameFinishedMessage
                {
                    GameId  = game.GameId,
                    EndTime = DateTime.UtcNow,
                    Status  = GameStatus.Finished,
                    Results = result.FinalResults ?? new()
                });

                _gameStateManager.RemoveGame(game.GameId);
            }
            else if (result.NextPlayerId.HasValue && result.NextMoveDeadlineSeconds.HasValue)
            {
                await _hubContext.Clients
                    .Group(game.GroupName)
                    .SendAsync(HubEvents.TurnChanged, result.NextPlayerId);

                ScheduleTurnTimeout(game, result.NextPlayerId.Value, result.NextMoveDeadlineSeconds.Value);
            }
        }, CancellationToken.None);
    }
}
