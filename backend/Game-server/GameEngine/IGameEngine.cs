using System.Collections.Generic;
using SC_GameServer.Messaging;
using SC_GameServer.Models;

namespace SC_GameServer.GameEngine;


public interface IGameEngine
{

    EngineBoardState CreateGame(int gameId, GameSettingsDto settings, List<GamePlayerDto> players);


    MoveResult ApplyMove(int gameId, int playerId, MoveRequest move);


    EngineBoardState Rehydrate(int gameId, GameSettingsDto settings, List<GamePlayerDto> players, IEnumerable<string> moveLogJsonInOrder);
}

public class EngineBoardState
{
    public int GameId { get; set; }
}

public class MoveRequest
{
    public string ActionType { get; set; } = null!; // "Reveal" | "Flag" | "Unflag" | "UsePowerUp" ...
    public int X { get; set; }
    public int Y { get; set; }
    public Dictionary<string, object>? Extra { get; set; } // powerup id, etc.
}

public class MoveResult
{
    public bool IsValid { get; set; }
    public string? InvalidReason { get; set; }

    public object? BroadcastPayload { get; set; }

    public string MoveLogJson { get; set; } = null!;

    public bool GameOver { get; set; }
    public List<PlayerResultDto>? FinalResults { get; set; }
}
