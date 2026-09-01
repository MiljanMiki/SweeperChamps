using System.Text.Json;
using SC_GameServer.Messaging;

namespace SC_GameServer.GameEngine;

public class TimeRushModeStrategy : IGameModeStrategy
{
    private readonly Dictionary<string, IMoveHandler> _handlers;

    public TimeRushModeStrategy()
    {
        _handlers = new List<IMoveHandler>
        {
            new RevealMoveHandler()
        }.ToDictionary(h => h.ActionType);
    }

    public bool IsActionAllowed(string actionType) => _handlers.ContainsKey(actionType);

    public int? GetCurrentTurnPlayerId(MinesweeperGameState state)
    {
        var active = state.Players.Where(p => !p.IsEliminated).ToList();
        return active.Count == 0
            ? null
            : active[state.CurrentTurnPlayerIndex % active.Count].PlayerId;
    }

    public MoveResult ApplyMove(MinesweeperGameState state, int playerId, MoveRequest move)
    {
        lock (state.Lock)
        {
            if (state.IsOver)
                return Invalid("Game is already over");

            if (playerId != GetCurrentTurnPlayerId(state))
                return Invalid("Not your turn");

            var cell = state.Board.Grid[move.X, move.Y];
            var result = _handlers[move.ActionType].Handle(state, playerId, move);
            if (!result.IsValid) return result;

            if (cell.IsMine)
            {
                state.GetPlayer(playerId)!.IsEliminated = true;
                state.AnyMineEverHit = true;
            }
            else
            {
                state.SafeCellsRevealedCount += ExtractRevealedCount(result);
            }

            state.CurrentTurnPlayerIndex++;
            return FinaliseResult(state, result);
        }
    }

    public MoveResult ApplyTimeout(MinesweeperGameState state, int playerId)
    {
        lock (state.Lock)
        {
            var player = state.GetPlayer(playerId);
            if (player is null || player.IsEliminated)
                return Invalid("Player not active");

            player.IsEliminated = true;
            state.AnyMineEverHit = true;
            state.CurrentTurnPlayerIndex++;

            var log = new { ActionType = ActionTypes.Timeout, playerId };
            var result = new MoveResult
            {
                IsValid = true,
                BroadcastPayload = log,
                MoveLogJson = JsonSerializer.Serialize(log)
            };

            return FinaliseResult(state, result);
        }
    }

    private MoveResult FinaliseResult(MinesweeperGameState state, MoveResult result)
    {
        result.GameOver = IsGameOver(state);

        if (result.GameOver)
        {
            state.IsOver = true;
            result.FinalResults = BuildResults(state);
        }
        else
        {
            result.NextPlayerId = GetCurrentTurnPlayerId(state);
            result.NextMoveDeadlineSeconds = state.Settings.StartTimeSeconds;
        }

        return result;
    }

    private static bool IsGameOver(MinesweeperGameState state)
    {
        if (state.Players.All(p => p.IsEliminated)) return true;

        bool redAlive = state.Players.Any(p => p.TeamColor == TeamColor.Red && !p.IsEliminated);
        bool blueAlive = state.Players.Any(p => p.TeamColor == TeamColor.Blue && !p.IsEliminated);
        if (!redAlive || !blueAlive) return true;

        if (state.SafeCellsRevealedCount >= state.Board.SafeCellCount) return true;

        return false;
    }

    private static List<PlayerResultDto> BuildResults(MinesweeperGameState state)
    {
        bool isDraw = state.SafeCellsRevealedCount >= state.Board.SafeCellCount
                      && !state.AnyMineEverHit;

        int redSurvivors = state.Players.Count(p => p.TeamColor == TeamColor.Red && !p.IsEliminated);
        int blueSurvivors = state.Players.Count(p => p.TeamColor == TeamColor.Blue && !p.IsEliminated);

        return state.Players.Select(p =>
        {
            int outcome = isDraw ? 0
                : (p.TeamColor == TeamColor.Red && redSurvivors > blueSurvivors) ? 1
                : (p.TeamColor == TeamColor.Blue && blueSurvivors > redSurvivors) ? 1
                : -1;

            return new PlayerResultDto { PlayerId = p.PlayerId, Score = outcome };
        }).ToList();
    }

    private static int ExtractRevealedCount(MoveResult result)
    {
        var json = JsonSerializer.Serialize(result.BroadcastPayload);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("RevealedCells", out var arr)
            ? arr.GetArrayLength()
            : 1;
    }

    private static MoveResult Invalid(string reason) =>
        new() { IsValid = false, InvalidReason = reason, MoveLogJson = string.Empty };
}