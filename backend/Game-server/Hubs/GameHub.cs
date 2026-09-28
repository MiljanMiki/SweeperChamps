using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SC_GameServer.GameEngine;
using SC_GameServer.Messaging;
using SC_GameServer.Models;
using SC_GameServer.Services;

namespace SC_GameServer.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly IGameStateManager _gameStateManager;
    private readonly IGameEngine _gameEngine;
    private readonly IRabbitMqPublisher _publisher;
    private readonly GameResultProcessor _resultProcessor;
    private readonly ILogger<GameHub> _logger;

    public GameHub(
        IGameStateManager gameStateManager,
        IGameEngine gameEngine,
        IRabbitMqPublisher publisher,
        GameResultProcessor resultProcessor,
        ILogger<GameHub> logger)
    {
        _gameStateManager = gameStateManager;
        _gameEngine = gameEngine;
        _publisher = publisher;
        _resultProcessor = resultProcessor;
        _logger = logger;
    }

    private int CurrentPlayerId =>
        int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new HubException("Missing player id claim"));

    // ─────────────────────────────────────────
    // Player joins their own game (the one they're a member of)
    // ─────────────────────────────────────────
    public async Task JoinGame()
    {
        var playerId = CurrentPlayerId;

        if (!_gameStateManager.TryGetGameForPlayer(playerId, out var game) || game is null)
        {
            await Clients.Caller.SendAsync(HubEvents.MoveRejected, "You are not in an active game.");
            return;
        }

        game.Connections[playerId] = Context.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, game.GroupName);

        var snapshot = BuildSnapshot(game);
        await Clients.Caller.SendAsync(HubEvents.BoardState, snapshot);
        await Clients.Group(game.GroupName).SendAsync(HubEvents.ScoreUpdate, BuildScoreList(game));
        await Clients.OthersInGroup(game.GroupName).SendAsync(HubEvents.PlayerConnected, playerId);

        _logger.LogInformation("Player {PlayerId} joined game {GameId}", playerId, game.GameId);
    }

    // ─────────────────────────────────────────
    // Spectator joins any game they can specify by id
    // ─────────────────────────────────────────
    public async Task JoinGameAsSpectator(int gameId)
    {
        var callerId = CurrentPlayerId;

        if (!_gameStateManager.TryGetGame(gameId, out var game) || game is null)
        {
            await Clients.Caller.SendAsync(HubEvents.MoveRejected, "Game not found or not active.");
            return;
        }

        // Add to SignalR group — spectators receive all broadcasts but cannot send moves
        await Groups.AddToGroupAsync(Context.ConnectionId, game.GroupName);

        var snapshot = BuildSnapshot(game);
        await Clients.Caller.SendAsync(HubEvents.BoardState, snapshot);
        await Clients.Caller.SendAsync(HubEvents.ScoreUpdate, BuildScoreList(game));
        await Clients.Caller.SendAsync(HubEvents.SpectatorJoined, new
        {
            gameId = game.GameId,
            spectatorPlayerId = callerId
        });

        _logger.LogInformation("Spectator {CallerId} joined game {GameId}", callerId, gameId);
    }

    // ─────────────────────────────────────────
    // Make a move (only real players; spectators get rejected)
    // ─────────────────────────────────────────
    public async Task MakeMove(MoveRequest move)
    {
        var playerId = CurrentPlayerId;

        if (!_gameStateManager.TryGetGameForPlayer(playerId, out var game) || game is null)
            throw new HubException("You are not a player in an active game");

        if (game.IsFinished)
            throw new HubException("Game has already ended");

        var result = _gameEngine.ApplyMove(game.GameId, playerId, move);

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

        await Clients.Group(game.GroupName).SendAsync(HubEvents.ScoreUpdate, BuildScoreList(game));

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

            await Clients.Group(game.GroupName).SendAsync(HubEvents.GameOver, result.FinalResults);

            await _publisher.PublishGameFinishedAsync(new GameFinishedMessage
            {
                GameId = game.GameId,
                EndTime = DateTime.UtcNow,
                Status = GameStatus.Finished,
                Results = result.FinalResults ?? new()
            });

            _gameStateManager.RemoveGame(game.GameId);
        }
        else if (result.NextPlayerId.HasValue && result.NextMoveDeadlineSeconds.HasValue)
        {
            await Clients.Group(game.GroupName).SendAsync(HubEvents.TurnChanged, result.NextPlayerId);
            _resultProcessor.ScheduleTurnTimeout(game, result.NextPlayerId.Value, result.NextMoveDeadlineSeconds.Value);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var playerId = CurrentPlayerId;
            if (_gameStateManager.TryGetGameForPlayer(playerId, out var game) && game is not null)
            {
                game.Connections.TryRemove(playerId, out _);
                _logger.LogInformation("Player {PlayerId} disconnected from game {GameId}", playerId, game.GameId);
            }
        }
        catch { }

        await base.OnDisconnectedAsync(exception);
    }

    // ─────────────────────────────────────────
    // Builders
    // ─────────────────────────────────────────
    private static BoardStateSnapshot BuildSnapshot(GameInstance game)
    {
        var state = (MinesweeperGameState)game.BoardState;

        int? currentTurn = null;
        if (game.Settings.WinCondition == WinCondition.TimeRush)
        {
            var active = state.Players.Where(p => !p.IsEliminated).ToList();
            if (active.Count > 0)
                currentTurn = active[state.CurrentTurnPlayerIndex % active.Count].PlayerId;
        }

        var players = state.Players
            .Select(p => new GamePlayerDto
            {
                PlayerId = p.PlayerId,
                Username = p.Username,
                TeamColor = p.TeamColor
            })
            .ToList();

        return new BoardStateSnapshot
        {
            GameId = game.GameId,
            Settings = game.Settings,
            Players = players,
            Cells = state.Board.ToSnapshot(),
            CurrentTurnPlayerId = currentTurn,
            IsGameOver = game.IsFinished,
        };
    }

    private static List<ScoreEntry> BuildScoreList(GameInstance game)
    {
        var state = (MinesweeperGameState)game.BoardState;
        return state.Players
            .Select(p => new ScoreEntry
            {
                PlayerId = p.PlayerId,
                Username = p.Username,
                TeamColor = p.TeamColor.ToString(),
                Score = p.Score,
                IsEliminated = p.IsEliminated
            })
            .ToList();
    }

    public class ScoreEntry
    {
        public int PlayerId { get; set; }
        public string Username { get; set; } = "";
        public string TeamColor { get; set; } = "";
        public int Score { get; set; }
        public bool IsEliminated { get; set; }
    }
}