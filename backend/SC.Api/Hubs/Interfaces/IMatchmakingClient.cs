namespace SC.Api.Hubs.Interfaces
{
    public class MatchFoundPayload
    {
        public int GameId { get; set; }
        public GameSettingsDto GameSettings { get; set; } = new();
        public List<GamePlayerDto> Players { get; set; } = new();
    }

    public class GameSettingsDto
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int NumberOfMines { get; set; }
        public int? StartTimeSeconds { get; set; }
        public int TeamSize { get; set; }
        public int WinCondition { get; set; }
        public bool HasPowerUps { get; set; }
    }

    public class GamePlayerDto
    {
        public int PlayerId { get; set; }
        public int TeamColor { get; set; }
    }

    public interface IMatchmakingClient
    {
        Task MatchmakingStarted();
        Task MatchFound();       // ← no payload needed anymore
        Task Error(string message);
    }
}