using Microsoft.Extensions.Configuration.UserSecrets;
using SC.Api.Services.Interfaces;
using SC.Domain.DataModels;
using SC.Domain.DTOs.Service;
using SC.Domain.Repositories.AsyncInterfaces;

namespace SC.Api.Services.Implementations
{
    public class UserStatsService : IUserStatsService
    {
        private readonly IUserStatsRepository _userStatsRepository;

        public UserStatsService(IUserStatsRepository userStatsRepository)
        {
            _userStatsRepository = userStatsRepository;
        }

        public async Task UpdatePlayerStats(List<UpdatePlayerStatsDto> dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            foreach (var item in dto)
            {
                var stat = await _userStatsRepository.GetStatAsync(item.UserId,item.GameSettingId,item.isRanked);
                if (stat == null)
                    throw new KeyNotFoundException($"Invalid {nameof(UserStats)} composite ID:\n" +
                        $"UserId: {item.UserId}\n" +
                        $"GameSettingId: {item.GameSettingId}\n" +
                        $"IsRanked: {item.isRanked}");

                if (item.isWin)
                    stat.Wins += 1;
                else
                    stat.Losses += 1;

                stat.PlayTime += item.gameDuration;
            }

            await _userStatsRepository.SaveChangesAsync();
        }
    }
}
