using SC.Domain.DataModels;
using SC.Domain.DTOs.GamePlayers;

namespace SC.Domain.Repositories.AsyncInterfaces
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
