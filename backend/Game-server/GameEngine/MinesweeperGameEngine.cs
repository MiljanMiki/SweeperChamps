using System.Collections.Concurrent;
using SC_GameServer.Messaging;

namespace SC_GameServer.GameEngine;

public class MinesweeperGameEngine : IGameEngine
{
    private readonly ConcurrentDictionary<int, MinesweeperGameState> _games = new();

    public GameCreationResult CreateGame(int gameId, GameSettingsDto settings, List<GamePlayerDto> players)
    {
        var board    = new Board(settings.Width, settings.Height, settings.NumberOfMines);
        var strategy = GameModeStrategyFactory.Create(settings.WinCondition);

        var state = new MinesweeperGameState
        {
            GameId   = gameId,
            Board    = board,
            Settings = settings,
            Strategy = strategy,
            Players  = players.Select(p => new PlayerRuntimeState
            {
                PlayerId  = p.PlayerId,
                TeamColor = p.TeamColor
            }).ToList()
        };

        _games[gameId] = state;

        return new GameCreationResult
        {
            BoardState          = state,
            FirstTurnPlayerId   = strategy.GetCurrentTurnPlayerId(state),
            MoveDeadlineSeconds = settings.StartTimeSeconds
        };
    }

    public MoveResult ApplyMove(int gameId, int playerId, MoveRequest move)
    {
        var state = GetState(gameId);

        if (!state.Strategy.IsActionAllowed(move.ActionType))
            return new MoveResult
            {
                IsValid       = false,
                InvalidReason = $"Action '{move.ActionType}' is not allowed in this game mode",
                MoveLogJson   = string.Empty
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
        throw new NotImplementedException("Rehydrate requires a persisted mine-layout seed.");
    }

    private MinesweeperGameState GetState(int gameId) =>
        _games.TryGetValue(gameId, out var state)
            ? state
            : throw new KeyNotFoundException($"No active game {gameId}");
}
