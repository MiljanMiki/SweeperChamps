using SC.Domain.DataModels;
using SC.Domain.DTOs.Service;

namespace SC.Api.Services.Interfaces
{
    public interface IGameService
    {
        Task<int> CreateGame(int gameSettingId, List<string> players, bool isRanked);

        //TODO: Also add information about game players, and add moves
        Task MarkGameFinished(MarkGameFinishedDto dto);
    }
}
