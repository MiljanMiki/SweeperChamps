using System.Text.Json;
using SC_GameServer.Messaging;

namespace SC_GameServer.GameEngine;

/// <summary>
/// TimeRush: turn-based, Reveal-only, on the shared board. Each player gets
/// Settings.StartTimeSeconds to move; hitting a mine or timing out
/// eliminates just that player - teammates keep playing. A team loses when
/// every one of its members is eliminated. If the whole safe board gets
/// revealed with zero mines ever hit, it's a draw.
///
/// ASSUMPTION (edge case not covered by the stated rules): if the safe board
/// is fully cleared *after* some players were already eliminated earlier in
/// the game (so "zero mines hit" no longer holds), the team with more
/// surviving players is treated as the winner; equal survivors = draw. Flag
/// this if a different tie-break is wanted.
/// </summary>
public class TimeRushModeStrategy : IGameModeStrategy
{
    public bool IsActionAllowed(string actionType) => actionType == "Reveal";

    public int? GetCurrentTurnPlayerId(MinesweeperGameState state)
    {
        if (state.Players.Count == 0) return null;
        return state.Players[state.CurrentTurnPlayerIndex].PlayerId;
    }

    public MoveResult ApplyMove(MinesweeperGameState state, int playerId, MoveRequest move)
    {
        lock (state.Lock)
        {
            if (state.IsOver) return Invalid("Game has already ended");

            if (GetCurrentTurnPlayerId(state) != playerId)
                return Invalid("It is not this player's turn");

            if (move.ActionType != "Reveal")
                return Invalid("Only Reveal is allowed in TimeRush");

            var board = state.Board;
            if (!board.InBounds(move.X, move.Y)) return Invalid("Cell out of bounds");

            var cell = board.Grid[move.X, move.Y];
            if (cell.State != CellState.Hidden) return Invalid("Cell already revealed");

            if (cell.IsMine)
            {
                cell.State = CellState.Revealed;
                state.AnyMineEverHit = true;
                return EliminatePlayer(state, playerId, "MineHit", cell);
            }

            var revealedCells = board.RevealWithCascade(cell.X, cell.Y);
            state.SafeCellsRevealedCount += revealedCells.Count;

            var payload = new
            {
                action = "Reveal",
                playerId,
                cells = revealedCells.Select(c => new { c.X, c.Y, c.AdjacentMineCount })
            };

            if (state.SafeCellsRevealedCount >= board.SafeCellCount)
                return FinalizeGameOver(state, payload);

            AdvanceTurn(state);
            return WithTurnInfo(state, new MoveResult
            {
                IsValid = true,
                BroadcastPayload = payload,
                MoveLogJson = JsonSerializer.Serialize(payload),
                GameOver = false
            });
        }
    }

    public MoveResult ApplyTimeout(MinesweeperGameState state, int playerId)
    {
        lock (state.Lock)
        {
            if (state.IsOver) return Invalid("Game has already ended");
            if (GetCurrentTurnPlayerId(state) != playerId)
                return Invalid("Timeout does not apply - it is not this player's turn");

            return EliminatePlayer(state, playerId, "Timeout", mineCell: null);
        }
    }

    private MoveResult EliminatePlayer(MinesweeperGameState state, int playerId, string reason, Cell? mineCell)
    {
        var player = state.GetPlayer(playerId)!;
        player.IsEliminated = true;

        var payload = new { action = reason, playerId, x = mineCell?.X, y = mineCell?.Y };

        bool teamFullyEliminated = state.Players
            .Where(p => p.TeamColor == player.TeamColor)
            .All(p => p.IsEliminated);

        if (teamFullyEliminated)
            return FinalizeGameOver(state, payload);

        AdvanceTurn(state);
        return WithTurnInfo(state, new MoveResult
        {
            IsValid = true,
            BroadcastPayload = payload,
            MoveLogJson = JsonSerializer.Serialize(payload),
            GameOver = false
        });
    }

    private MoveResult FinalizeGameOver(MinesweeperGameState state, object lastActionPayload)
    {
        state.IsOver = true;

        List<PlayerResultDto> results;
        bool cleanDraw = !state.AnyMineEverHit && state.SafeCellsRevealedCount >= state.Board.SafeCellCount;

        if (cleanDraw)
        {
            results = state.Players.Select(p => new PlayerResultDto { PlayerId = p.PlayerId, Score = 0 }).ToList();
        }
        else
        {
            // Winner = team with more surviving players; equal survivors = draw.
            var survivorsByTeam = state.Players
                .Where(p => !p.IsEliminated)
                .GroupBy(p => p.TeamColor)
                .ToDictionary(g => g.Key, g => g.Count());

            int maxSurvivors = survivorsByTeam.Values.DefaultIfEmpty(0).Max();
            var winningTeams = survivorsByTeam.Where(kv => kv.Value == maxSurvivors).Select(kv => kv.Key).ToList();

            results = state.Players.Select(p => new PlayerResultDto
            {
                PlayerId = p.PlayerId,
                // 1 = win, 0 = draw/loss. Refine once persistence semantics for TimeRush results are pinned down.
                Score = winningTeams.Count == 1 && p.TeamColor == winningTeams[0] ? 1 : 0
            }).ToList();
        }

        return new MoveResult
        {
            IsValid = true,
            BroadcastPayload = lastActionPayload,
            MoveLogJson = JsonSerializer.Serialize(lastActionPayload),
            GameOver = true,
            FinalResults = results
        };
    }

    private void AdvanceTurn(MinesweeperGameState state)
    {
        int n = state.Players.Count;
        if (n == 0) return;

        for (int i = 1; i <= n; i++)
        {
            int candidate = (state.CurrentTurnPlayerIndex + i) % n;
            if (!state.Players[candidate].IsEliminated)
            {
                state.CurrentTurnPlayerIndex = candidate;
                return;
            }
        }
        // No active players left - game should already be over by this point.
    }

    private MoveResult WithTurnInfo(MinesweeperGameState state, MoveResult result)
    {
        result.NextPlayerId = GetCurrentTurnPlayerId(state);
        result.NextMoveDeadlineSeconds = state.Settings.StartTimeSeconds;
        return result;
    }

    private static MoveResult Invalid(string reason) => new() { IsValid = false, InvalidReason = reason, MoveLogJson = "" };
}
