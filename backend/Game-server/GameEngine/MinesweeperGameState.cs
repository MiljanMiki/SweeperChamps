using SC_GameServer.Messaging;

namespace SC_GameServer.GameEngine;

public class PlayerRuntimeState
{
    public int PlayerId { get; init; }
    public string TeamColor { get; init; } = null!;
    public int Score { get; set; }
    public bool IsEliminated { get; set; } // TimeRush only
}

/// <summary>
/// Concrete runtime state for one active Minesweeper game.
/// </summary>
public class MinesweeperGameState : EngineBoardState
{
    public Board Board { get; init; } = null!;
    public GameSettingsDto Settings { get; init; } = null!;
    public List<PlayerRuntimeState> Players { get; init; } = new();
    public IGameModeStrategy Strategy { get; init; } = null!;

    /// <summary>Guards all mutation of this game's state. Race mode allows concurrent moves from different players on the same board, so every strategy method takes this lock.</summary>
    public object Lock { get; } = new();

    // ---- Race mode bookkeeping ----
    /// <summary>Revealed cells + correctly-flagged mines. Compared against Board.TotalCells to end the game in O(1) instead of rescanning the grid.</summary>
    public int ResolvedCellCount { get; set; }

    // ---- TimeRush mode bookkeeping ----
    /// <summary>Index into Players (fixed join order) for whose turn it is.</summary>
    public int CurrentTurnPlayerIndex { get; set; }
    public int SafeCellsRevealedCount { get; set; }
    public bool AnyMineEverHit { get; set; }

    public bool IsOver { get; set; }

    public PlayerRuntimeState? GetPlayer(int playerId) => Players.Find(p => p.PlayerId == playerId);
}
