using SC.Api.Services.Interfaces;
using SC.Domain.Repositories.AsyncInterfaces;

namespace SC.Api.Services.Implementations
{
    public class GameSettingsService : IGameSettingsService
    {
        private readonly IGameSettingRepository _gameSettingsRepository;

        public GameSettingsService(IGameSettingRepository gameSettingsRepository)
        {
            _gameSettingsRepository = gameSettingsRepository;
        }

        public async Task<int> GetRequiredPlayersAsync(int gameSettingsId)
        {
            var settings = await _gameSettingsRepository.GetAsync(gameSettingsId);
            if (settings == null)
            {
                throw new KeyNotFoundException($"Game settings with ID {gameSettingsId} not found.");
            }

            return settings.TeamSize * 2;
        }
    }
}
