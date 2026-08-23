using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SC_GameServer.GameEngine;
using SC_GameServer.Services;

namespace SC_GameServer.Hubs;

[Authorize] // assumes JWT auth already configured (same scheme as the web API)
public class GameHub : Hub
{
    private readonly IGameStateManager _gameStateManager;
    private readonly IGameEngine _gameEngine;
    private readonly IGameResultProcessor _resultProcessor;
    private readonly ILogger<GameHub> _logger;

    public GameHub(
        IGameStateManager gameStateManager,
        IGameEngine gameEngine,
        IGameResultProcessor resultProcessor,
        ILogger<GameHub> logger)
    {
        _gameStateManager = gameStateManager;
        _gameEngine = gameEngine;
        _resultProcessor = resultProcessor;
        _logger = logger;
    }

    private int CurrentPlayerId =>
        int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new HubException("Missing player id claim"));

    /// <summary>
    /// Client calls this right after connecting (or reconnecting) to attach
    /// to their game's group and register their current connectionId.
    /// </summary>
    public async Task JoinGame(int gameId)
    {
        if (!_gameStateManager.TryGetGame(gameId, out var game) || game is null)
            throw new HubException("Game not found or not active");

        var playerId = CurrentPlayerId;
        if (!game.Players.Any(p => p.PlayerId == playerId))
            throw new HubException("Player is not part of this game");

        game.Connections[playerId] = Context.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, game.GroupName);

        await Clients.OthersInGroup(game.GroupName).SendAsync("PlayerConnected", playerId);

        // TODO: send the joining player the current board state so a
        // reconnecting client can resync (needs the engine to expose a
        // per-player/team view of current board state - not built yet).
    }

    /// <summary>
    /// Client submits a move. Server validates via the engine; on success the
    /// result is broadcast to the group, persisted via RabbitMQ, and (for
    /// TimeRush) the next player's turn timer is scheduled - all handled by
    /// IGameResultProcessor so this path matches the timeout path exactly.
    /// </summary>
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
            await Clients.Caller.SendAsync("MoveRejected", result.InvalidReason);
            return;
        }

        await _resultProcessor.ProcessAsync(game, playerId, result);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Connection drop doesn't end the game - the player can reconnect and
        // call JoinGame again. TimeRush's turn timer keeps running regardless
        // (a disconnected player on the clock still times out and is
        // eliminated - that's the "auto-lose" behavior you asked for).
        await base.OnDisconnectedAsync(exception);
    }
}