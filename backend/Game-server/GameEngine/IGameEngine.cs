using SC_GameServer.Messaging;

namespace SC_GameServer.GameEngine;

/// <summary>
/// GameServer only ever talks to a game's rules through this interface -
/// board state, mine layout, and win-condition logic all live behind it.
/// </summary>
public interface IGameEngine
{
    /// <summary>Creates a fresh board/state for a new game, called once when the GameCreatedMessage arrives.</summary>
    GameCreationResult CreateGame(int gameId, GameSettingsDto settings, List<GamePlayerDto> players);

    /// <summary>Validates and applies a move. Deterministic given the same state + move, so it can be replayed from the move log.</summary>
    MoveResult ApplyMove(int gameId, int playerId, MoveRequest move);

    /// <summary>Called when a player's per-move timer expires. No-op/invalid for modes without turn timers (e.g. Race).</summary>
    MoveResult ApplyTimeout(int gameId, int playerId);

    /// <summary>Rebuilds in-memory state by replaying a persisted move log - used to recover active games after a restart.</summary>
    EngineBoardState Rehydrate(int gameId, GameSettingsDto settings, List<GamePlayerDto> players, IEnumerable<string> moveLogJsonInOrder);
}

/// <summary>Opaque handle to a game's runtime state; GameServer just stores a reference, never reaches into it.</summary>
public class EngineBoardState
{
    public int GameId { get; set; }
}

public class GameCreationResult
{
    public EngineBoardState BoardState { get; set; } = null!;

    /// <summary>Whose turn it is right now, if this mode has turn order (null for Race).</summary>
    public int? FirstTurnPlayerId { get; set; }
    public int? MoveDeadlineSeconds { get; set; }
}

public class MoveRequest
{
    public string ActionType { get; set; } = null!; // "Reveal" | "Flag" | "Unflag"
    public int X { get; set; }
    public int Y { get; set; }
    public Dictionary<string, object>? Extra { get; set; } // reserved for future powerups
}

public class MoveResult
{
    public bool IsValid { get; set; }
    public string? InvalidReason { get; set; }

    /// <summary>What to broadcast to clients - engine/mode-defined shape.</summary>
    public object? BroadcastPayload { get; set; }

    /// <summary>Full move + result, serialized for persistence via MoveMadeMessage.</summary>
    public string MoveLogJson { get; set; } = null!;

    public bool GameOver { get; set; }
    public List<PlayerResultDto>? FinalResults { get; set; }

    /// <summary>TimeRush only: whose turn is next, and how long they have to move.</summary>
    public int? NextPlayerId { get; set; }
    public int? NextMoveDeadlineSeconds { get; set; }
}