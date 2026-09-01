using System.Collections.Concurrent;
using SC_GameServer.Models;

namespace SC_GameServer.Services;

public interface IGameStateManager
{
    void          AddGame(GameInstance instance);
    bool          TryGetGame(int gameId, out GameInstance? instance);
    void          RemoveGame(int gameId);
}

public class GameStateManager : IGameStateManager
{
    private readonly ConcurrentDictionary<int, GameInstance> _games = new();

    public void AddGame(GameInstance instance)                          => _games[instance.GameId] = instance;
    public bool TryGetGame(int gameId, out GameInstance? instance)      => _games.TryGetValue(gameId, out instance);
    public void RemoveGame(int gameId)                                  => _games.TryRemove(gameId, out _);
}
