using System.Collections.Concurrent;
using SC_GameServer.Messaging;

namespace SC_GameServer.GameEngine;

/// <summary>
/// Orchestrator - builds the board once per game and delegates all rule
/// logic to whichever IGameModeStrategy matches the game's WinCondition.
/// This class never branches on Race vs TimeRush itself (strategy pattern).
/// </summary>
public class MinesweeperGameEngine : IGameEngine
{
    private readonly ConcurrentDictionary<int, MinesweeperGameState> _games = new();

    public GameCreationResult CreateGame(int gameId, GameSettingsDto settings, List<GamePlayerDto> players)
    {
        var board = new Board(settings.Width, settings.Height, settings.NumberOfMines);
        var strategy = GameModeStrategyFactory.Create(settings.WinCondition);

        var state = new MinesweeperGameState
        {
            GameId = gameId,
            Board = board,
            Settings = settings,
            Strategy = strategy,
            Players = players.Select(p => new PlayerRuntimeState
            {
                PlayerId = p.PlayerId,
                TeamColor = p.TeamColor
            }).ToList()
        };

        _games[gameId] = state;

        return new GameCreationResult
        {
            BoardState = state,
            FirstTurnPlayerId = strategy.GetCurrentTurnPlayerId(state),
            MoveDeadlineSeconds = settings.StartTimeSeconds
        };
    }

    public MoveResult ApplyMove(int gameId, int playerId, MoveRequest move)
    {
        var state = GetState(gameId);

        if (!state.Strategy.IsActionAllowed(move.ActionType))
            return new MoveResult
            {
                IsValid = false,
                InvalidReason = $"'{move.ActionType}' is not allowed in this game mode",
                MoveLogJson = ""
            };

        return state.Strategy.ApplyMove(state, playerId, move);
    }

    public MoveResult ApplyTimeout(int gameId, int playerId)
    {
        var state = GetState(gameId);
        return state.Strategy.ApplyTimeout(state, playerId);
    }

    public EngineBoardState Rehydrate(int gameId, GameSettingsDto settings, List<GamePlayerDto> players, IEnumerable<string> moveLogJsonInOrder)
    {
        // TODO: replay each persisted move log entry through the same
        // ApplyMove path used live, so recovery produces identical state.
        // This needs the mine layout to be deterministic (a persisted seed),
        // since Board currently generates mines randomly per CreateGame call
        // - see the write-up for the decision this needs from you.
        throw new NotImplementedException("Rehydrate needs a persisted mine-layout seed - see write-up");
    }

    private MinesweeperGameState GetState(int gameId) =>
        _games.TryGetValue(gameId, out var state)
            ? state
            : throw new KeyNotFoundException($"No active game {gameId}");
}
