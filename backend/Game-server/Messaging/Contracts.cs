namespace SC_GameServer.Messaging;

// ---------- Enums (shared domain values) ----------

public enum WinCondition { Race, TimeRush }
public enum TeamColor    { Red, Blue }
public enum GameStatus   { Finished, InProgress, Aborted, Terminated }

// ---------- Inbound: API -> GameServer ----------

public class GameCreatedMessage
{
    public int              GameId       { get; set; }
    public GameSettingsDto  GameSettings { get; set; } = null!;
    public List<GamePlayerDto> Players   { get; set; } = new();
}

public class GameSettingsDto
{
    public int          Width            { get; set; }
    public int          Height           { get; set; }
    public int          NumberOfMines    { get; set; }
    public int?         StartTimeSeconds { get; set; }
    public int          TeamSize         { get; set; }
    public WinCondition WinCondition     { get; set; }
    public bool         HasPowerUps      { get; set; }
}

public class GamePlayerDto
{
    public int       PlayerId  { get; set; }
    public TeamColor TeamColor { get; set; }
}

// ---------- Outbound: GameServer -> API ----------

public class MoveMadeMessage
{
    public int      GameId      { get; set; }
    public int      PlayerId    { get; set; }
    public DateTime Timestamp   { get; set; }
    public string   MoveLogJson { get; set; } = null!;
}

public class GameFinishedMessage
{
    public int                  GameId  { get; set; }
    public DateTime             EndTime { get; set; }
    public GameStatus           Status  { get; set; }
    public List<PlayerResultDto> Results { get; set; } = new();
}

public class PlayerResultDto
{
    public int PlayerId { get; set; }
    public int Score    { get; set; }
}
