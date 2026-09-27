using SC.Domain.DTOs.Service;

namespace SC.Api.Services.Interfaces
{
    public interface IGamePlayerService
    {
        Task RecordPlayerResults(List<GameFinishedPlayerStatsDto> playerResults);
    }
}
