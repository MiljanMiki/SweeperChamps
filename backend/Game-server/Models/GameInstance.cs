using System.Collections.Concurrent;
using SC_GameServer.GameEngine;
using SC_GameServer.Messaging;

namespace SC_GameServer.Models;

/// <summary>Everything the server tracks about one active game.</summary>
public class GameInstance
{
    public int GameId { get; init; }
    public GameSettingsDto Settings { get; init; } = null!;
    public List<GamePlayerDto> Players { get; init; } = new();
    public EngineBoardState BoardState { get; set; } = null!;

    /// <summary>playerId -> current SignalR connectionId (last one wins on reconnect).</summary>
    public ConcurrentDictionary<int, string> Connections { get; } = new();

    public bool IsFinished { get; set; }

    /// <summary>TimeRush only: cancelling this aborts the pending timeout for the current turn (a move arrived in time).</summary>
    public CancellationTokenSource? TurnTimerCts { get; set; }

    public string GroupName => $"game-{GameId}";
}