using System.Text.Json;
using SC_GameServer.Messaging;

namespace SC_GameServer.GameEngine;

public class RaceModeStrategy : IGameModeStrategy
{
    private readonly Dictionary<string, IMoveHandler> _handlers;

    public RaceModeStrategy()
    {
        _handlers = new List<IMoveHandler>
        {
            new RevealMoveHandler(),
            new FlagMoveHandler(),
            new UnflagMoveHandler()
        }.ToDictionary(h => h.ActionType);
    }

    public bool IsActionAllowed(string actionType) => _handlers.ContainsKey(actionType);

    public int? GetCurrentTurnPlayerId(MinesweeperGameState state) => null;

    public MoveResult ApplyMove(MinesweeperGameState state, int playerId, MoveRequest move)
    {
        lock (state.Lock)
        {
            if (state.IsOver)
                return Invalid("Game is already over");

            if (state.GetPlayer(playerId) is null)
                return Invalid("Player not in this game");

            var result = _handlers[move.ActionType].Handle(state, playerId, move);
            if (!result.IsValid) return result;

            result.GameOver = state.ResolvedCellCount >= state.Board.TotalCells;
            if (result.GameOver)
            {
                state.IsOver = true;
                result.FinalResults = BuildResults(state);
            }

            return result;
        }
    }

    public MoveResult ApplyTimeout(MinesweeperGameState state, int playerId) =>
        Invalid("Race mode has no turn timer");

    private static List<PlayerResultDto> BuildResults(MinesweeperGameState state)
    {
        return state.Players
            .Select(p => new PlayerResultDto { PlayerId = p.PlayerId, Score = p.Score })
            .ToList();
    }

    private static MoveResult Invalid(string reason) =>
        new() { IsValid = false, InvalidReason = reason, MoveLogJson = string.Empty };
}