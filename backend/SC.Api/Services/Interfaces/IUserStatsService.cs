using SC.Domain.DTOs.Service;

namespace SC.Api.Services.Interfaces
{
    public interface IUserStatsService
    {
        Task UpdatePlayerStats(List<UpdatePlayerStatsDto> dto);
    }
}
