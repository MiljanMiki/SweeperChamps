using Humanizer;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;

namespace SC_Backend.Repositories
{
    public class GameSettingRepository : IGameSettingRepository
    {
        private readonly ApplicationDbContext _context;

        public GameSettingRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public void Add(GameSetting entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.GameSettings.Add(entity);
        }

        public void Delete(GameSetting entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.GameSettings.Remove(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Update(GameSetting entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.GameSettings.Update(entity);
        }

        public async Task<IEnumerable<GameSetting>> GetAllAsync()
        {
            return await _context.GameSettings.AsNoTracking().ToListAsync();
        }

        public async Task<GameSetting?> GetAsync(int id)
        {
            return await _context.GameSettings.FindAsync(id);
        }
        public async Task<GameSetting?> GetOrCreateSettingAsync(GameSetting setting)
        {
            ArgumentNullException.ThrowIfNull(setting);

            var existingSetting = await _context.GameSettings.AsNoTracking()
                .FirstOrDefaultAsync(gs =>
                gs.Width == setting.Width &&
                gs.Height == setting.Height &&
                gs.NumberOfMines == setting.NumberOfMines &&
                gs.StartTimeSeconds == setting.StartTimeSeconds &&
                gs.TeamSize == setting.TeamSize &&
                gs.WinCondition == setting.WinCondition &&
                gs.HasPowerUps == setting.HasPowerUps);

            if (existingSetting != null)
            {
                return existingSetting;
            }
            else
            {
                Add(setting);
                await _context.SaveChangesAsync();
                return null;
            }
        }
        public async Task<IEnumerable<GameSetting>> GetStandardModesAsync()
        {
            var standardModes = await _context.GameSettings
                .Where(gs => !gs.HasPowerUps && gs.TeamSize == 1)
                .OrderBy(gs => gs.Width * gs.Height) // Order by board size
                .Take(3) // Beginner, Intermediate, Expert
                .ToListAsync();

            return standardModes;
        }
    }
}
