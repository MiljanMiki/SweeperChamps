using System.Collections.Concurrent;
using SC_GameServer.GameEngine;
using SC_GameServer.Messaging;

namespace SC_GameServer.Models;

public class GameInstance
{
    public int                               GameId       { get; init; }
    public GameSettingsDto                   Settings     { get; init; } = null!;
    public List<GamePlayerDto>               Players      { get; init; } = new();
    public EngineBoardState                  BoardState   { get; set; }  = null!;
    public ConcurrentDictionary<int, string> Connections  { get; }       = new();
    public CancellationTokenSource?          TurnTimerCts { get; set; }
    public bool                              IsFinished   { get; set; }

    public string GroupName => $"game-{GameId}";
}
