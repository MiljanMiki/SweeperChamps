using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SC_GameServer.GameEngine;
using SC_GameServer.Messaging;
using SC_GameServer.Services;

namespace SC_GameServer.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly IGameStateManager    _gameStateManager;
    private readonly IGameEngine          _gameEngine;
    private readonly IRabbitMqPublisher   _publisher;
    private readonly GameResultProcessor  _resultProcessor;
    private readonly ILogger<GameHub>     _logger;

    public GameHub(
        IGameStateManager gameStateManager,
        IGameEngine gameEngine,
        IRabbitMqPublisher publisher,
        GameResultProcessor resultProcessor,
        ILogger<GameHub> logger)
    {
        _gameStateManager = gameStateManager;
        _gameEngine       = gameEngine;
        _publisher        = publisher;
        _resultProcessor  = resultProcessor;
        _logger           = logger;
    }

    private int CurrentPlayerId =>
        int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new HubException("Missing player id claim"));

    public async Task JoinGame(int gameId)
    {
        if (!_gameStateManager.TryGetGame(gameId, out var game) || game is null)
            throw new HubException("Game not found or not active");

        var playerId = CurrentPlayerId;

        if (!game.Players.Any(p => p.PlayerId == playerId))
            throw new HubException("Player is not part of this game");

        game.Connections[playerId] = Context.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, game.GroupName);
        await Clients.OthersInGroup(game.GroupName).SendAsync(HubEvents.PlayerConnected, playerId);
    }

    public async Task MakeMove(int gameId, MoveRequest move)
    {
        if (!_gameStateManager.TryGetGame(gameId, out var game) || game is null)
            throw new HubException("Game not found or not active");

        var playerId = CurrentPlayerId;

        if (!game.Players.Any(p => p.PlayerId == playerId))
            throw new HubException("Player is not part of this game");

        if (game.IsFinished)
            throw new HubException("Game has already ended");

        var result = _gameEngine.ApplyMove(gameId, playerId, move);

        if (!result.IsValid)
        {
            await Clients.Caller.SendAsync(HubEvents.MoveRejected, result.InvalidReason);
            return;
        }

        await Clients.Group(game.GroupName).SendAsync(HubEvents.MoveMade, new
        {
            playerId,
            payload = result.BroadcastPayload
        });

        await _publisher.PublishMoveMadeAsync(new MoveMadeMessage
        {
            GameId      = gameId,
            PlayerId    = playerId,
            Timestamp   = DateTime.UtcNow,
            MoveLogJson = result.MoveLogJson
        });

        if (result.GameOver)
        {
            game.IsFinished = true;

            await Clients.Group(game.GroupName).SendAsync(HubEvents.GameOver, result.FinalResults);

            await _publisher.PublishGameFinishedAsync(new GameFinishedMessage
            {
                GameId  = gameId,
                EndTime = DateTime.UtcNow,
                Status  = GameStatus.Finished,
                Results = result.FinalResults ?? new()
            });

            _gameStateManager.RemoveGame(gameId);
        }
        else if (result.NextPlayerId.HasValue && result.NextMoveDeadlineSeconds.HasValue)
        {
            await Clients.Group(game.GroupName)
                .SendAsync(HubEvents.TurnChanged, result.NextPlayerId);

            _resultProcessor.ScheduleTurnTimeout(game, result.NextPlayerId.Value, result.NextMoveDeadlineSeconds.Value);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
