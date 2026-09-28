namespace SC_GameServer.Messaging;

// ---------- Enums ----------
public enum WinCondition { Race, TimeRush }
public enum TeamColor { Red, Blue }
public enum GameStatus { Finished, InProgress, Aborted, Terminated }

// ---------- Inbound: API -> GameServer ----------
public class GameCreatedMessage
{
    public int GameId { get; set; }
    public GameSettingsDto GameSettings { get; set; } = null!;
    public List<GamePlayerDto> Players { get; set; } = new();
}

public class GameSettingsDto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int NumberOfMines { get; set; }
    public int? StartTimeSeconds { get; set; }
    public int TeamSize { get; set; }
    public WinCondition WinCondition { get; set; }
    public bool HasPowerUps { get; set; }
}

public class GamePlayerDto
{
    public int PlayerId { get; set; }
    public string Username { get; set; } = "";   // ← new
    public TeamColor TeamColor { get; set; }
}

// ---------- Outbound: GameServer -> Client (SignalR) ----------
public class BoardCellSnapshot
{
    public int X { get; set; }
    public int Y { get; set; }
    public string State { get; set; } = "Hidden";
    public int AdjacentMineCount { get; set; }
    public bool IsMine { get; set; }
    public int? RevealedByPlayerId { get; set; }
}

public class BoardStateSnapshot
{
    public int GameId { get; set; }
    public GameSettingsDto Settings { get; set; } = null!;
    public List<GamePlayerDto> Players { get; set; } = new();
    public List<BoardCellSnapshot> Cells { get; set; } = new();
    public int? CurrentTurnPlayerId { get; set; }
    public bool IsGameOver { get; set; }
    public List<PlayerResultDto>? FinalResults { get; set; }
}

// ---------- Outbound: GameServer -> API ----------
public class MoveMadeMessage
{
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public DateTime Timestamp { get; set; }
    public string MoveLogJson { get; set; } = null!;
}

public class GameFinishedMessage
{
    public int GameId { get; set; }
    public DateTime EndTime { get; set; }
    public GameStatus Status { get; set; }   // serialized as "Finished"
    public List<PlayerResultDto> Results { get; set; } = new();
}

public class PlayerResultDto
{
    public int PlayerId { get; set; }
    public int Score { get; set; }
}