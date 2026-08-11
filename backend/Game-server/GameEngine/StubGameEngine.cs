using System.Collections.Generic;
using System.Text.Json;
using SC_GameServer.Messaging;

namespace SC_GameServer.GameEngine;


public class StubGameEngine : IGameEngine
{
    public EngineBoardState CreateGame(int gameId, GameSettingsDto settings, List<GamePlayerDto> players)
        => new EngineBoardState { GameId = gameId };

    public MoveResult ApplyMove(int gameId, int playerId, MoveRequest move)
    {
        return new MoveResult
        {
            IsValid = true,
            BroadcastPayload = new { move.ActionType, move.X, move.Y, playerId },
            MoveLogJson = JsonSerializer.Serialize(move),
            GameOver = false
        };
    }

    public EngineBoardState Rehydrate(int gameId, GameSettingsDto settings, List<GamePlayerDto> players, IEnumerable<string> moveLogJsonInOrder)
        => new EngineBoardState { GameId = gameId };
}
