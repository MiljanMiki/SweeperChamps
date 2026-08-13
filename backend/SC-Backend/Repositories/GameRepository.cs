using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.Games;

namespace SC_Backend.Repositories
{
    public class GameRepository : IGameRepository
    {
        private readonly ApplicationDbContext _context;

        public GameRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Game>> GetAllAsync()
        {
            return await _context.Games.AsNoTracking().ToListAsync();
        }

        public async Task<Game?> GetAsync(int id)
        {
            return await _context.Games.FindAsync(id);
        }
        public void Add(Game entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var gameSetting = _context.GameSettings.Find(entity.GameSettingsId);
            if (gameSetting == null)
            {
                throw new KeyNotFoundException($"FK {entity.GameSettingsId} of {nameof(GameSetting)} does not map to any row.");
            }

            _context.Games.Add(entity);
        }

        public void Update(Game entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.Games.Update(entity);
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        

        public void Delete(Game entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            _context.Games.Remove(entity);
        }

        ///  <summary>
        /// Filters games based on status and the date of the start of the game. If date is omitted, it will be filtered only by status.
        /// If date is not omitted then one of day, month or year parameters must be set to true or  ArgumentException is thrown. Only the first parameter set 
        /// to true is considered in the query.
        /// </summary>
        /// <param name="status">Current status of the game.</param>
        /// <param name="date">Date by which the games will be filtered. If none of the following parameters is not set to true the query returns BadRequest: day, month, year</param>
        /// <param name="day">If set to true games will be filtered by day only. Month and year will not be considered</param>
        /// <param name="month">If set to true games will be filtered by month only.Day and year will not be considered</param>
        /// <param name="year">If set to true games will be filtered by year only. Day and month will not be considered</param>
        /// <returns>All games that satisfy the criteria.</returns>
        public async Task<IEnumerable<Game>> FilterGameByStatusAndDateAsync(GameStatuses status, DateTime? date = null, bool day = false, bool month = false, bool year = false)
        {
            var query = _context.Games.Where(g => g.Status == status);

            if (date != null)
            {
                if (day)
                    query = query.Where(g => g.StartTime.Date == date.Value.Date);
                else if (month)
                    query = query.Where(g => g.StartTime.Month == date.Value.Month);
                else if (year)
                    query = query.Where(g => g.StartTime.Year == date.Value.Year);
                else
                    throw new ArgumentException("Day, month or year must be specified for date filtering.");
            }

            return await query.ToListAsync();
        }
        public async Task<IEnumerable<Game>> FilterByDurationAsync(int durationSeconds, bool longer)
        {
            var query = _context.Games.Where(g => g.Status == GameStatuses.Finished && g.EndTime != null);//za svaki slucaj i null check
            query = longer ?
                query.Where(g => EF.Functions.DateDiffSecond(g.StartTime, g.EndTime) > durationSeconds) :
                query.Where(g => EF.Functions.DateDiffSecond(g.StartTime, g.EndTime) <= durationSeconds);

            return await query.ToListAsync();
        }
    }
}
