namespace SC.Api.Services.Interfaces
{
    public interface IMatchmakingService
    {
        Task QueueUserAsync(string userId, int gameSettingsId, bool isRanked);
        void PublishCancelTicket(string userId);
    }
}
