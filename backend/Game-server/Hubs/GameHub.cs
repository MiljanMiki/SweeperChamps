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

        // Send score update too so the scoreboard renders immediately
        await Clients.Group(game.GroupName).SendAsync(HubEvents.ScoreUpdate, BuildScoreList(game));

        await Clients.OthersInGroup(game.GroupName).SendAsync(HubEvents.PlayerConnected, playerId);

        _logger.LogInformation("Player {PlayerId} joined game {GameId}", playerId, game.GameId);
    }

    public async Task MakeMove(MoveRequest move)
    {
        var playerId = CurrentPlayerId;

        if (!_gameStateManager.TryGetGameForPlayer(playerId, out var game) || game is null)
            throw new HubException("You are not in an active game");

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

        // ── LIVE SCORES ── broadcast after every valid move
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

    // ─────────────────────────────────────────────
    // Snapshot builder — now includes usernames
    // ─────────────────────────────────────────────
    private static BoardStateSnapshot BuildSnapshot(Models.GameInstance game)
    {
        var state = (MinesweeperGameState)game.BoardState;

        int? currentTurn = null;
        if (game.Settings.WinCondition == WinCondition.TimeRush)
        {
            var active = state.Players.Where(p => !p.IsEliminated).ToList();
            if (active.Count > 0)
                currentTurn = active[state.CurrentTurnPlayerIndex % active.Count].PlayerId;
        }

        // Rebuild player DTOs from the runtime state so we get usernames from the state (source of truth)
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

    // ─────────────────────────────────────────────
    // Live score list
    // ─────────────────────────────────────────────
    private static List<ScoreEntry> BuildScoreList(Models.GameInstance game)
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

    public Task<bool> HasActiveGame()
    {
        var playerId = CurrentPlayerId;
        var has = _gameStateManager.TryGetGameForPlayer(playerId, out _);
        return Task.FromResult(has);
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