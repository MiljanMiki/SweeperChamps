using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.Repositories.AsyncInterfaces;
using System.Configuration;
using System.Threading.Tasks;

namespace SC_Backend.Repositories.AsyncImplementations
{
    public class UserStatsRepository :BaseAsyncRepository<UserStats>, IUserStatsRepository
    {
        public UserStatsRepository(ApplicationDbContext context) : base(context) { }

        public override Task<UserStats?> GetAsync(int id)
        {
            throw new NotImplementedException("This function cannot be implemented because of incompatible keys");
        }
        public override void Add(UserStats entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            if (Context.Users.Find(entity.UserId) == null)
                throw new KeyNotFoundException($"{nameof(User)} does not exist with ID {entity.UserId}");
            if (Context.GameSettings.Find(entity.GameSettingId) == null)
                throw new KeyNotFoundException($"{nameof(GameSetting)} does not exist with ID {entity.GameSettingId}");

            DbSet.Add(entity);
        }
        public async Task<UserStats?> GetStatAsync(int userID, int gameSettingID, bool isRanked)
        {
            await CheckFK(userID, gameSettingID);
            return await DbSet
               .FirstOrDefaultAsync(s => s.UserId == userID && s.GameSettingId == gameSettingID && s.IsRanked == isRanked);
        }


        public async Task<UserStats?> GetStatsWithLoadedPropertiesAsync(int userID, int gameSettingID, bool isRanked)
        {
            await CheckFK(userID, gameSettingID);
            return await DbSet
                .AsNoTracking()
                .Include(s => s.GameSetting)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userID && s.GameSettingId == gameSettingID && s.IsRanked == isRanked);
        }

        public async Task<IEnumerable<UserStats>> GetAllStatsOfUserAsync(int userID, int? gameSettingID = null, bool? isRanked = null, bool loadNav = false)
        {
            await CheckFK(userID, gameSettingID);

            var query = DbSet.AsNoTracking().Where(s => s.UserId == userID);

            if (gameSettingID.HasValue && gameSettingID.Value > 0)
                query = query.Where(s => s.GameSettingId == gameSettingID);

            if(isRanked.HasValue)
                query = query.Where(s => isRanked.Value ? s.IsRanked == true : s.IsRanked == false);

            if (loadNav)
            {
                if (gameSettingID.HasValue)
                    query = query.Include(s => s.GameSetting);
                query = query.Include(s => s.User);
            }  

            return await query.ToListAsync();
        }

        //public async Task<IEnumerable<UserStats>> GetHighestWinrateSettingsAsync(int topCount)
        //{
        //    return await DbSet
        //        .OrderByDescending(s=> s.GamesPlayed / s.Wins)
        //        .Take(topCount).ToListAsync();
        //}

        public async Task<IEnumerable<UserStats>> GetTopPlayersForSettingAsync(int gameSettingId, bool isRanked, int topCount)
        {
            await CheckFK(null, gameSettingId);

            return await DbSet
                .AsNoTracking()
                .Where(s => s.GameSettingId == gameSettingId && s.IsRanked == isRanked)
                .OrderByDescending(s => s.GamesPlayed == 0 ? 0 : (double)s.Wins / s.GamesPlayed)
                .Include(s => s.User)
                .Take(topCount).ToListAsync();
        }

        public async Task RecordMatchResultAsync(int userId, int gameSettingId, bool isRanked, bool isWin, long matchDuration)
        {
            await CheckFK(userId, gameSettingId);

            var stat = await GetStatAsync(userId, gameSettingId, isRanked);
            if (stat == null)
                throw new ArgumentException($"No {nameof(UserStats)} can be found with the given FKs");

            stat.GamesPlayed += 1;
            if (isWin)
                stat.Wins += 1;
            else
                stat.Losses += 1;
            stat.PlayTime += matchDuration;

            await SaveChangesAsync();
        }

        private async Task CheckFK(int? userID, int? gameSettingID)
        {
            if (userID.HasValue)
            {
                var user = await Context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UsersId == userID.Value);
                if (user == null)
                    throw new KeyNotFoundException($"{nameof(User)} does not exist with ID {userID}");
            }

            if (gameSettingID.HasValue)
            {
                var setting = await Context.GameSettings.AsNoTracking().FirstOrDefaultAsync(s => s.GameSettingsId == gameSettingID.Value);
                if (setting == null)
                    throw new KeyNotFoundException($"{nameof(GameSetting)} does not exist with ID {gameSettingID}");
            }
        }

    }
}
