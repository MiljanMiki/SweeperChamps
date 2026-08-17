using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using System.Threading.Tasks;

namespace SC_Backend.Repositories
{
    public class UserStatsRepository : IUserStatsRepository
    {
        private readonly ApplicationDbContext _context;


        public UserStatsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserStats?> GetAsync(int id)
        {
            throw new NotImplementedException("This function cannot be implemented because of incompatible keys");
        }
        public async Task<IEnumerable<UserStats>> GetAllAsync()
        {
            return await _context.UserStats.AsNoTracking().ToListAsync();
        }
        public void Add(UserStats entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            if (_context.Users.Find(entity.UserId) == null)
                throw new KeyNotFoundException($"{nameof(User)} does not exist with ID {entity.UserId}");
            if (_context.GameSettings.Find(entity.GameSettingId) == null)
                throw new KeyNotFoundException($"{nameof(GameSetting)} does not exist with ID {entity.GameSetting}");

            _context.UserStats.Add(entity);
        }

        public void Delete(UserStats entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.UserStats.Remove(entity);
        }
        public void Update(UserStats entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.UserStats.Update(entity);
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<UserStats?> GetStatAsync(int userID, int gameSettingID, bool isRanked)
        {
            return await _context.UserStats
               .FirstOrDefaultAsync(s => s.UserId == userID && s.GameSettingId == gameSettingID && s.IsRanked == isRanked);
        }


        public async Task<UserStats?> GetStatsWithLoadedPropertiesAsync(int userID, int gameSettingID, bool isRanked)
        {
            return await _context.UserStats
                .AsNoTracking()
                .Include(s => s.GameSetting)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userID && s.GameSettingId == gameSettingID && s.IsRanked == isRanked);
        }

        public async Task<IEnumerable<UserStats>> GetAllStatsOfUserAsync(int userID, int? gameSettingID = null, bool isRanked = false, bool loadNav = false)
        {
            var query = _context.UserStats.Where(s => s.UserId == userID).AsNoTracking();

            if (gameSettingID.HasValue && gameSettingID.Value > 0)
                query = query.Where(s => s.GameSettingId == gameSettingID);

            query = query.Where(s => isRanked ? s.IsRanked == true : s.IsRanked == false);

            if(loadNav)
                query = query.Include(s=>s.GameSetting).Include(s=>s.User);

            return await query.ToListAsync();
        }

        //public async Task<IEnumerable<UserStats>> GetHighestWinrateSettingsAsync(int topCount)
        //{
        //    return await _context.UserStats
        //        .OrderByDescending(s=> s.GamesPlayed / s.Wins)
        //        .Take(topCount).ToListAsync();
        //}

        public async Task<IEnumerable<UserStats>> GetTopPlayersForSettingAsync(int gameSettingId, bool isRanked, int topCount)
        {
            return await _context.UserStats
                .AsNoTracking()
                .Where(s => s.GameSettingId == gameSettingId && s.IsRanked == isRanked)
                .OrderByDescending(s => s.GamesPlayed / s.Wins)
                .Include(s => s.User)
                .Take(topCount).ToListAsync();

        }

        public async Task RecordMatchResultAsync(int userId, int gameSettingId, bool isRanked, bool isWin, long matchDuration)
        {
            if (userId <= 0 || gameSettingId <= 0)
                throw new KeyNotFoundException("One or both of FKs are less than or equal to 0");

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

    }
}
