using SC_Backend.DataModels;
using SC_Backend.DTOs.GamePlayers;

namespace SC_Backend.Repositories.AsyncInterfaces
{
    public interface IGamePlayerRepository : IAsyncRepository<GamePlayer>
    {
        Task<IEnumerable<GamePlayer>> GetAllPlayersFromGameAsync(int gameId);
        Task<IEnumerable<Game>> GetGamesFromPlayerAsync(int playerID, bool orderByScore = false);
        Task<IEnumerable<Game>> GamesBetweenPlayersAsync(int[] playerIDs);
        Task<GamePlayer?> GetLoadedGamePlayerAsync(int id);
        Task<IEnumerable<Game>> GetGamesFromPlayerWithSettingAsync(int playerID, int settingID);
        Task<IEnumerable<GamePlayer>> GetUserMatchHistoryAsync(int userId, int page, int pageSize);
        Task UpdatePlayerResultsAsync(IEnumerable<GamePlayer> finalPlayerStats);
        Task<int> GetTotalScoreForUserAsync(int userId);
    }
}
