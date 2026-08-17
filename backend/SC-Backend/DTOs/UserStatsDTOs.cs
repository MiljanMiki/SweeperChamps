using SC_Backend.DataModels;
using System.ComponentModel.DataAnnotations.Schema;
using SC_Backend.DTOs.Users;
using SC_Backend.DTOs.GameSettings;

namespace SC_Backend.DTOs.UserStats
{
    public record UserStatDTO
    {
        public bool IsRanked { get; set; }
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public long PlayTime { get; set; }
    }

    public record FullStatDTO
    {
        public int GameSettingId { get; set; }
        public int UserId { get; set; }
        public bool IsRanked { get; set; }
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public long PlayTime { get; set; }
    }

    public record LoadedStatDto
    {
        public bool IsRanked { get; set; }
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public long PlayTime { get; set; }
        public UserDTO? UserSummary { get; set; }
        public GameSettingDto? SettingSummary { get; set; }
    }

    public record LeaderboardDTO
    {
        public string Username { get; set; } = null!;
        public short? Elo { get; set; }
        public int GameSettingId { get; set; }
        public UserStatDTO UserStat { get; set; } = null!;
        public double WinRatePercentage { get; set; }
    }





}
