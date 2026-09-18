using System.Text.Json;

namespace SC_GameServer.GameEngine;

public class RevealMoveHandler : IMoveHandler
{
    public string ActionType => ActionTypes.Reveal;

    public MoveResult Handle(MinesweeperGameState state, int playerId, MoveRequest move)
    {
        var cell = state.Board.Grid[move.X, move.Y];
        var player = state.GetPlayer(playerId)!;

        if (cell.State != CellState.Hidden)
            return Invalid("Cell is already revealed or flagged");

        if (cell.IsMine)
        {
            cell.State = CellState.Revealed;
            state.ResolvedCellCount++;
            player.Score -= 50;

            var mineLog = new { ActionType = ActionTypes.Reveal, move.X, move.Y, HitMine = true, playerId };
            return new MoveResult
            {
                IsValid = true,
                BroadcastPayload = mineLog,
                MoveLogJson = JsonSerializer.Serialize(mineLog)
            };
        }

        var revealed = state.Board.RevealWithCascade(move.X, move.Y);
        state.ResolvedCellCount += revealed.Count;
        player.Score += revealed.Count;

        var log = new
        {
            ActionType = ActionTypes.Reveal,
            move.X,
            move.Y,
            HitMine = false,
            RevealedCells = revealed.Select(c => new { c.X, c.Y, c.AdjacentMineCount })
        };

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