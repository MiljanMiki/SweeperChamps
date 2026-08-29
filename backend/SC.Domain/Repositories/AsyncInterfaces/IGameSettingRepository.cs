
using SC.Domain.DataModels;
using SC.Domain.DTOs.GameSettings;

namespace SC.Domain.Repositories.AsyncInterfaces
{
    public interface IGameSettingRepository : IAsyncRepository<GameSetting>
    {
        Task<GameSetting?> GetOrCreateSettingAsync(GameSetting setting);
        Task<IEnumerable<GameSetting>> GetStandardModesAsync();

        Task<GameSetting?> GetMostPlayedSettingAsync();
    }
}
