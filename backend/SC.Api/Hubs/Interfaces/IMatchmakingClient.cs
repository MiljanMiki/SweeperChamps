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
        public string WinCondition { get; set; } = "Race";
        public bool HasPowerUps { get; set; }
    }

    public class GamePlayerDto
    {
        public int PlayerId { get; set; }
        public string Username { get; set; } = "";     // ← new
        public string TeamColor { get; set; } = "Red";
    }

    public interface IMatchmakingClient
    {
        Task MatchmakingStarted();
        Task MatchFound(MatchFoundPayload payload);
        Task Error(string message);
    }
}