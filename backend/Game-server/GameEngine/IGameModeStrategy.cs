namespace SC_GameServer.GameEngine;

/// <summary>
/// Strategy pattern: each win condition (Race, TimeRush) implements its own
/// rules for which actions are legal, how a move is scored/resolved, and
/// when the game ends. MinesweeperGameEngine is mode-agnostic - it never
/// branches on Race vs TimeRush itself, it just delegates to whichever
/// strategy the game was created with.
/// </summary>
public interface IGameModeStrategy
{
    bool IsActionAllowed(string actionType);

    /// <summary>Whose move is it right now, if this mode has turn order (null = no restriction, e.g. Race).</summary>
    int? GetCurrentTurnPlayerId(MinesweeperGameState state);

    MoveResult ApplyMove(MinesweeperGameState state, int playerId, MoveRequest move);

    /// <summary>Called when a player's per-move timer expires. Modes without turn timers should return an invalid result.</summary>
    MoveResult ApplyTimeout(MinesweeperGameState state, int playerId);
}

internal static class GameModeStrategyFactory
{
    public static IGameModeStrategy Create(string winCondition) => winCondition switch
    {
        "Race" => new RaceModeStrategy(),
        "TimeRush" => new TimeRushModeStrategy(),
        _ => throw new ArgumentOutOfRangeException(nameof(winCondition), winCondition, "Unknown win condition")
    };
}
