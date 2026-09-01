namespace SC_GameServer.GameEngine;

public interface IMoveHandler
{
    string ActionType { get; }
    MoveResult Handle(MinesweeperGameState state, int playerId, MoveRequest move);
}