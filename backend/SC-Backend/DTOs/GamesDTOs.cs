using SC_Backend.DataModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SC_Backend.DTOs.Games
{
    public record GetGameDto
    {
        public int GamesId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public GameStatuses Status { get; set; }
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
        public int GameSettingsId { get; set; }

    }

}
