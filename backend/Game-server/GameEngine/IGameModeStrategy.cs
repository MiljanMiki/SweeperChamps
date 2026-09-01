using SC_GameServer.Messaging;

namespace SC_GameServer.GameEngine;

public interface IGameModeStrategy
{
    bool  IsActionAllowed(string actionType);
    int?  GetCurrentTurnPlayerId(MinesweeperGameState state);
    MoveResult ApplyMove(MinesweeperGameState state, int playerId, MoveRequest move);
    MoveResult ApplyTimeout(MinesweeperGameState state, int playerId);
}

internal static class GameModeStrategyFactory
{
    public static IGameModeStrategy Create(WinCondition winCondition) => winCondition switch
    {
        WinCondition.Race     => new RaceModeStrategy(),
        WinCondition.TimeRush => new TimeRushModeStrategy(),
        _ => throw new ArgumentOutOfRangeException(nameof(winCondition), winCondition, "Unknown win condition")
    };
}
