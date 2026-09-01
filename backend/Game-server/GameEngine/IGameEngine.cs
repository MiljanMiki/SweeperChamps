using SC_GameServer.Messaging;

namespace SC_GameServer.GameEngine;

public interface IGameEngine
{
    GameCreationResult CreateGame(int gameId, GameSettingsDto settings, List<GamePlayerDto> players);
    MoveResult         ApplyMove(int gameId, int playerId, MoveRequest move);
    MoveResult         ApplyTimeout(int gameId, int playerId);
    EngineBoardState   Rehydrate(int gameId, GameSettingsDto settings, List<GamePlayerDto> players, IEnumerable<string> moveLogJsonInOrder);
}

public class EngineBoardState
{
    public int GameId { get; set; }
}

public class GameCreationResult
{
    public EngineBoardState BoardState          { get; set; } = null!;
    public int?             FirstTurnPlayerId   { get; set; }
    public int?             MoveDeadlineSeconds { get; set; }
}

public class MoveRequest
{
    public string                       ActionType { get; set; } = null!;
    public int                          X          { get; set; }
    public int                          Y          { get; set; }
    public Dictionary<string, object>?  Extra      { get; set; }
}

public class MoveResult
{
    public bool                  IsValid              { get; set; }
    public string?               InvalidReason        { get; set; }
    public object?               BroadcastPayload     { get; set; }
    public string                MoveLogJson          { get; set; } = null!;
    public bool                  GameOver             { get; set; }
    public List<PlayerResultDto>? FinalResults        { get; set; }
    public int?                  NextPlayerId         { get; set; }
    public int?                  NextMoveDeadlineSeconds { get; set; }
}
