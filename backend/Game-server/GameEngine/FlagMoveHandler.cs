using System.Text.Json;

namespace SC_GameServer.GameEngine;

public class FlagMoveHandler : IMoveHandler
{
    public string ActionType => ActionTypes.Flag;

    public MoveResult Handle(MinesweeperGameState state, int playerId, MoveRequest move)
    {
        var cell = state.Board.Grid[move.X, move.Y];
        var player = state.GetPlayer(playerId)!;

        if (cell.State != CellState.Hidden)
            return Invalid("Can only flag a hidden cell");

        cell.State = CellState.Flagged;

        if (cell.IsMine)
        {
            player.Score += 10;
            state.ResolvedCellCount++;
        }

        var log = new { ActionType = ActionTypes.Flag, move.X, move.Y, WasMine = cell.IsMine, playerId };
        return new MoveResult
        {
            IsValid = true,
            BroadcastPayload = log,
            MoveLogJson = JsonSerializer.Serialize(log)
        };
    }

    private static MoveResult Invalid(string reason) =>
        new() { IsValid = false, InvalidReason = reason, MoveLogJson = string.Empty };
}