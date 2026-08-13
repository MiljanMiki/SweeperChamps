using SC_Backend.DataModels;
using SC_Backend.DTOs.GamePlayers;

namespace SC_Backend.Repositories
{
    public interface IGamePlayerAsyncRepository : IAsyncRepository<GamePlayer>
    {

        Task<IEnumerable<GamePlayer>> GetAllPlayersFromGameAsync(int gameId);
        Task<IEnumerable<Game>> GetAllGamesFromPlayerAsync(int playerID, bool orderByScore = false);
        Task<IEnumerable<Game>> GamesBetweenPlayersAsync(int[] playerIDs);
    }
}
