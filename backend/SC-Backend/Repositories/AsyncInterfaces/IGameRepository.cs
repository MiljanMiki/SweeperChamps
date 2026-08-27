using Microsoft.AspNetCore.Mvc;
using SC_Backend.DataModels;
using SC_Backend.DTOs.Games;

namespace SC_Backend.Repositories.AsyncInterfaces
{
    public interface IGameRepository : IAsyncRepository<Game>
    {
        Task<Game?> GetLoadedGame(int id);
        Task<IEnumerable<Game>> GetAllGamesWithSetting(int settingID);
        Task<IEnumerable<Game>> FilterGameByStatusAndDateAsync(GameStatuses status, DateTime? date = null, bool day = false, bool month = false, bool year = false);
        Task<IEnumerable<Game>> FilterByDurationAsync(int durationSeconds, bool longer);
        Task<IEnumerable<Game>> GetLiveGamesAsync(int limit);
        Task MarkGameAsFinishedAsync(int gameId, int durationSeconds, TeamColors winningTeam);
        Task<IEnumerable<Game>> GetGamesWithPlayer(int playerID, int limit,bool ranked = false);
    }
}
