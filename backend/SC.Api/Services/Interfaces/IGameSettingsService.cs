namespace SC.Api.Services.Interfaces
{
    public interface IGameSettingsService
    {
        Task<int> GetRequiredPlayersAsync(int gameSettingsId);
    }
}
