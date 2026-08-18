using Microsoft.AspNetCore.Mvc;
using SC_Backend.DataModels;
using SC_Backend.DTOs.GameSettings;

namespace SC_Backend.Repositories.AsyncInterfaces
{
    public interface IGameSettingRepository : IAsyncRepository<GameSetting>
    {
        Task<GameSetting?> GetOrCreateSettingAsync(GameSetting setting);
        Task<IEnumerable<GameSetting>> GetStandardModesAsync();

        Task<GameSetting?> GetMostPlayedSettingAsync();
    }
}
