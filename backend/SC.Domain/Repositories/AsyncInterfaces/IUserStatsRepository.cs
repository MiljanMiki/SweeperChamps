using SC.Domain.DataModels;
using System.Threading.Tasks;

namespace SC.Domain.Repositories.AsyncInterfaces
{
    public interface IUserStatsRepository : IAsyncRepository<UserStats>
    {
        Task<UserStats?> GetStatAsync(int userID, int gameSettingID, bool isRanked);
        Task<UserStats?> GetStatsWithLoadedPropertiesAsync(int userID, int gameSettingID,bool isRanked);
        Task<IEnumerable<UserStats>> GetAllStatsOfUserAsync(int userID, int? gameSettingID = null, bool? isRanked = null, bool loadNav =false);

        //Task<> GetOverallUserStatsAsync(int userId);//ovo bi mozda terbalo controller da implementira
        //Task<IEnumerable<UserStats>> GetHighestWinrateSettingsAsync(int topCount);

        //ionako kaze getTopPlayers, vraca userStats koji sadrzi user. Kontroler iz ovoga moze da sta mu treba
        Task<IEnumerable<UserStats>> GetTopPlayersForSettingAsync(int gameSettingId, bool isRanked, int topCount);

        Task RecordMatchResultAsync(int userId, int gameSettingId, bool isRanked, bool isWin, long matchDuration);
    }
}
