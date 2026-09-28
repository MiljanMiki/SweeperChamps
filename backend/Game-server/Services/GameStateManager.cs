using System.Collections.Concurrent;
using SC_GameServer.Models;

namespace SC_GameServer.Services;

public interface IGameStateManager
{
    void AddGame(GameInstance instance);
    bool TryGetGame(int gameId, out GameInstance? instance);
    bool TryGetGameForPlayer(int playerId, out GameInstance? instance);
    void RemoveGame(int gameId);
}

public class GameStateManager : IGameStateManager
{
    private readonly ConcurrentDictionary<int, GameInstance> _games = new();
    private readonly ConcurrentDictionary<int, int> _playerToGame = new();

    public void AddGame(GameInstance instance)
    {
        _games[instance.GameId] = instance;
        foreach (var p in instance.Players)
            _playerToGame[p.PlayerId] = instance.GameId;
    }

    public bool TryGetGame(int gameId, out GameInstance? instance) =>
        _games.TryGetValue(gameId, out instance);

    public bool TryGetGameForPlayer(int playerId, out GameInstance? instance)
    {
        instance = null;
        if (!_playerToGame.TryGetValue(playerId, out var gameId)) return false;
        return _games.TryGetValue(gameId, out instance);
    }

    public void RemoveGame(int gameId)
    {
        if (_games.TryRemove(gameId, out var game))
            foreach (var p in game.Players)
                _playerToGame.TryRemove(p.PlayerId, out _);
    }
}