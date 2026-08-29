namespace SC.Api.Hubs.Interfaces
{
    public interface IMatchmakingClient
    {
        // Acknowledges that the ticket made it to RabbitMQ
        Task MatchmakingStarted();

        // Pushed when the background worker creates the lobby
        Task MatchFound(int gameId);

        // Fallback for validation errors
        Task Error(string message);
    }
}
