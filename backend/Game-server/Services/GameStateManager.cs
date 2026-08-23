using System.Collections.Concurrent;
using SC_GameServer.Models;

namespace SC_GameServer.Services;

public interface IGameStateManager
{
    void AddGame(GameInstance instance);
    bool TryGetGame(int gameId, out GameInstance? instance);
    void RemoveGame(int gameId);
}

/// <summary>
/// Single source of truth for which games are currently active on this
/// instance. Single-process for now (v1) - if you scale to multiple
/// GameServer instances later, this is the piece that needs a distributed
/// "gameId -> instance owner" lookup (e.g. Redis) in front of it.
/// </summary>
public class GameStateManager : IGameStateManager
{
    private readonly ConcurrentDictionary<int, GameInstance> _games = new();

    public void AddGame(GameInstance instance) => _games[instance.GameId] = instance;

    public bool TryGetGame(int gameId, out GameInstance? instance) => _games.TryGetValue(gameId, out instance);

    public void RemoveGame(int gameId) => _games.TryRemove(gameId, out _);
}
