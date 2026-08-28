using System.Text.Json;
using SC_GameServer.Messaging;

namespace SC_GameServer.GameEngine;

/// <summary>
/// Race mode: no turns - everyone can Reveal/Flag concurrently on the shared
/// board. Scoring: +1 per empty cell opened, +10 per mine correctly flagged,
/// -50 per mine clicked. Flagging a non-mine scores nothing and doesn't
/// resolve the cell (the player still needs to unflag + reveal it later).
/// Ends when every cell is resolved (revealed, or correctly flagged if a
/// mine) - tracked with a running counter instead of rescanning the board.
/// </summary>
public class RaceModeStrategy : IGameModeStrategy
{
    private const int PointsPerEmptyCell = 1;
    private const int PointsPerCorrectFlag = 10;
    private const int PointsPerMineHit = -50;

    public bool IsActionAllowed(string actionType) => actionType is "Reveal" or "Flag" or "Unflag";

    // No turn order in Race - anyone can move at any time.
    public int? GetCurrentTurnPlayerId(MinesweeperGameState state) => null;

    public MoveResult ApplyMove(MinesweeperGameState state, int playerId, MoveRequest move)
    {
        lock (state.Lock)
        {
            if (state.IsOver) return Invalid("Game has already ended");

            var player = state.GetPlayer(playerId);
            if (player is null) return Invalid("Player is not part of this game");

            var board = state.Board;
            if (!board.InBounds(move.X, move.Y)) return Invalid("Cell out of bounds");

            var cell = board.Grid[move.X, move.Y];

            return move.ActionType switch
            {
                "Reveal" => HandleReveal(state, player, cell),
                "Flag" => HandleFlag(state, player, cell),
                "Unflag" => HandleUnflag(cell),
                _ => Invalid($"Unknown action '{move.ActionType}'")
            };
        }
    }

    public MoveResult ApplyTimeout(MinesweeperGameState state, int playerId) =>
        Invalid("Race mode has no per-move timers");

    private MoveResult HandleReveal(MinesweeperGameState state, PlayerRuntimeState player, Cell cell)
    {
        if (cell.State != CellState.Hidden) return Invalid("Cell already resolved");

        if (cell.IsMine)
        {
            cell.State = CellState.Revealed; // exploded, but the game keeps going in Race mode
            state.ResolvedCellCount++;
            player.Score += PointsPerMineHit;

            return Finalize(state, new
            {
                action = "MineHit",
                x = cell.X,
                y = cell.Y,
                playerId = player.PlayerId,
                scoreDelta = PointsPerMineHit
            });
        }

        var revealedCells = state.Board.RevealWithCascade(cell.X, cell.Y);
        state.ResolvedCellCount += revealedCells.Count;
        player.Score += revealedCells.Count * PointsPerEmptyCell;

        return Finalize(state, new
        {
            action = "Reveal",
            playerId = player.PlayerId,
            scoreDelta = revealedCells.Count * PointsPerEmptyCell,
            cells = revealedCells.Select(c => new { c.X, c.Y, c.AdjacentMineCount })
        });
    }

    private MoveResult HandleFlag(MinesweeperGameState state, PlayerRuntimeState player, Cell cell)
    {
        if (cell.State != CellState.Hidden) return Invalid("Cell already resolved or flagged");

        cell.State = CellState.Flagged;

        if (cell.IsMine)
        {
            // A correctly flagged mine is resolved and locked - see HandleUnflag.
            state.ResolvedCellCount++;
            player.Score += PointsPerCorrectFlag;

            return Finalize(state, new
            {
                action = "FlagCorrect",
                x = cell.X,
                y = cell.Y,
                playerId = player.PlayerId,
                scoreDelta = PointsPerCorrectFlag
            });
        }

        // Wrong flag: no score, cell isn't resolved (doesn't count toward
        // ending the game) - player can unflag it and reveal it properly.
        return Finalize(state, new
        {
            action = "FlagIncorrect",
            x = cell.X,
            y = cell.Y,
            playerId = player.PlayerId,
            scoreDelta = 0
        });
    }

    private MoveResult HandleUnflag(Cell cell)
    {
        if (cell.State != CellState.Flagged) return Invalid("Cell is not flagged");
        if (cell.IsMine) return Invalid("A correctly flagged mine is locked and cannot be unflagged");

        cell.State = CellState.Hidden;

        var payload = new { action = "Unflag", x = cell.X, y = cell.Y };
        return new MoveResult
        {
            IsValid = true,
            BroadcastPayload = payload,
            MoveLogJson = JsonSerializer.Serialize(payload),
            GameOver = false
        };
    }

    private MoveResult Finalize(MinesweeperGameState state, object payload)
    {
        bool gameOver = state.ResolvedCellCount >= state.Board.TotalCells;
        state.IsOver = gameOver;

        return new MoveResult
        {
            IsValid = true,
            BroadcastPayload = payload,
            MoveLogJson = JsonSerializer.Serialize(payload),
            GameOver = gameOver,
            FinalResults = gameOver ? BuildResults(state) : null
        };
    }

    private List<PlayerResultDto> BuildResults(MinesweeperGameState state) =>
        state.Players.Select(p => new PlayerResultDto { PlayerId = p.PlayerId, Score = p.Score }).ToList();

    private static MoveResult Invalid(string reason) => new() { IsValid = false, InvalidReason = reason, MoveLogJson = "" };
}
