using SC_GameServer.Messaging;

namespace SC_GameServer.GameEngine;

public class PlayerRuntimeState
{
    public int       PlayerId    { get; init; }
    public TeamColor TeamColor   { get; init; }
    public int       Score       { get; set; }
    public bool      IsEliminated { get; set; }
}

public class MinesweeperGameState : EngineBoardState
{
    public Board                Board    { get; init; } = null!;
    public GameSettingsDto      Settings { get; init; } = null!;
    public List<PlayerRuntimeState> Players { get; init; } = new();
    public IGameModeStrategy    Strategy { get; init; } = null!;
    public object               Lock     { get; } = new();

    // Race
    public int  ResolvedCellCount { get; set; }

    // TimeRush
    public int  CurrentTurnPlayerIndex  { get; set; }
    public int  SafeCellsRevealedCount  { get; set; }
    public bool AnyMineEverHit          { get; set; }

    public bool IsOver { get; set; }

    public PlayerRuntimeState? GetPlayer(int playerId) =>
        Players.Find(p => p.PlayerId == playerId);
}
