using Humanizer;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;

namespace SC_Backend.Repositories
{
    public class GameSettingRepository :BaseAsyncRepository<GameSetting>, IGameSettingRepository
    {
        public GameSettingRepository(ApplicationDbContext context) :base(context) {}
        public async Task<GameSetting?> GetOrCreateSettingAsync(GameSetting setting)
        {
            ArgumentNullException.ThrowIfNull(setting);

            var existingSetting = await DbSet.AsNoTracking()
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
                await Context.SaveChangesAsync();
                return null;
            }
        }
        public async Task<IEnumerable<GameSetting>> GetStandardModesAsync()
        {
            var standardModes = await DbSet
                .Where(gs => !gs.HasPowerUps && gs.TeamSize == 1)
                .OrderBy(gs => gs.Width * gs.Height) // Order by board size
                .Take(3) // Beginner, Intermediate, Expert
                .ToListAsync();

            return standardModes;
        }
    }
}
