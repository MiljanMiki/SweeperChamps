using SC_Backend.DataModels;
using System.ComponentModel.DataAnnotations.Schema;

namespace SC_Backend.DTOs.GamePlayers
{
    public record PutGamePlayerRequestDto
    {
        //public int GameId { get; set; }

        //public int PlayerId { get; set; }

        public TeamColors TeamColor { get; set; }

        public int Score { get; set; }
    }

    public record GamePlayerDto
    {
        public int GameId { get; set; }

        public int PlayerId { get; set; }

        public TeamColors TeamColor { get; set; }

        public int Score { get; set; }
    }

    public record GetAllPlayersRequestDto
    {
        public int PlayerId { get; set; }
        public string Username { get; set; }

        public short? Elo { get; set; }

        public TeamColors TeamColor { get; set; }

        public int Score { get; set; }
    }

    public record AllGamesTwoPlayersRequestDto
    {
        public int GamesId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public GameStatuses Status { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public int NumberOfMines { get; set; }

        public List<PlayerSummaryDto> PlayerSummary { get; set; }

    }

    public record PlayerSummaryDto
    {
        public int PlayerId { get; set; }
        public string Username { get; set; } = null!;
        public string TeamColor { get; set; } = null!;
        public int Score { get; set; }

        public short? Elo { get; set; }
    }



    public record GameSummaryDto
    {
        public int GamesId { get; set; }
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public GameStatuses Status { get; set; }

        public int Score { get; set; }
    }
}
