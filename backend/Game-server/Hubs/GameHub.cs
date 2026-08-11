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
    private readonly IGameStateManager _gameStateManager;
    private readonly IGameEngine _gameEngine;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<GameHub> _logger;

    public GameHub(
        IGameStateManager gameStateManager,
        IGameEngine gameEngine,
        IRabbitMqPublisher publisher,
        ILogger<GameHub> logger)
    {
        _gameStateManager = gameStateManager;
        _gameEngine = gameEngine;
        _publisher = publisher;
        _logger = logger;
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

        // Let the rest of the group know this player (re)connected.
        await Clients.OthersInGroup(game.GroupName).SendAsync("PlayerConnected", playerId);

        // TODO: send the joining player the current board state so a
        // reconnecting client can resync (needs engine support to serialize
        // current state for a given player/team's view of the board).
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
            // Only tell the caller, not the whole group.
            await Clients.Caller.SendAsync("MoveRejected", result.InvalidReason);
            return;
        }

        await Clients.Group(game.GroupName).SendAsync("MoveMade", new
        {
            playerId,
            payload = result.BroadcastPayload
        });

        _publisher.PublishMoveMade(new MoveMadeMessage
        {
            GameId = gameId,
            PlayerId = playerId,
            Timestamp = DateTime.UtcNow,
            MoveLogJson = result.MoveLogJson
        });

        if (result.GameOver)
        {
            game.IsFinished = true;

            await Clients.Group(game.GroupName).SendAsync("GameOver", result.FinalResults);

            _publisher.PublishGameFinished(new GameFinishedMessage
            {
                GameId = gameId,
                EndTime = DateTime.UtcNow,
                Status = "Finished",
                Results = result.FinalResults ?? new List<PlayerResultDto>()
            });

            _gameStateManager.RemoveGame(gameId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Connection drop doesn't end the game - the player can reconnect and
        // call JoinGame again. If you want an "opponent disconnected" toast,
        // look up which game this connectionId belonged to and notify the
        // group here (needs a connectionId -> (gameId, playerId) reverse map
        // if you want O(1) lookup instead of scanning active games).
        await base.OnDisconnectedAsync(exception);
    }
}
