using SC.Domain.DataModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SC.Domain.DTOs.Games
{
    public record GameDto
    {
        public int GamesId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public GameStatuses Status { get; set; }

        public bool IsRanked { get; set; }

        public int? DurationSeconds { get; set; }

        public TeamColors? WinningTeam { get; set; }

        public int GameSettingsId { get; set; }
    }

    public record PutGameDto
    {
        public DateTime? EndTime { get; set; }

        public GameStatuses Status { get; set; }
    }

    public record PostGameDto
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public GameStatuses Status { get; set; }
        public bool IsRanked { get; set; }
        public int? DurationSeconds { get; set; }
        public TeamColors? WinningTeam { get; set; }
        public int GameSettingsId { get; set; }


    }

}
