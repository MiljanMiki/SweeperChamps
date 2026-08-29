using SC.Domain.DataModels;
using SC.Domain.DTOs.GamePlayers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SC.Domain.DTOs.Users
{
    public record UserDTO
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateOnly Datecreated { get; set; }
        public short? Elo { get; set; }
        public UserRoles UserRole { get; set; }
    }

    public record UserUpdateDTO
    {
        public string Username { get; set; } = null!;

        public string Email { get; set; } = null!;
        public short? Elo { get; set; }
        public UserRoles UserRole { get; set; }
    }

    public record UserCreateDTO
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public UserRoles UserRole { get; set; }
    }

    public record LoadedUserDTO
    {
        public UserDTO UserData { get; set; } = null!;

        public List<GamesHistoryDTO>? GameHistory {get;set;}

        public List<UserStatsDTO>? Stats { get; set; }


    }

    public record GamesHistoryDTO
    {
        public int GameId { get; set; }

        public int PlayerId { get; set; }

        public TeamColors TeamColor { get; set; }

        public int Score { get; set; }
    }

    public record UserStatsDTO
    {
        public int GameSettingId { get; set; }

        public bool IsRanked { get; set; }

        public int GamesPlayed { get; set; }

        public int Wins { get; set; }

        public int Losses { get; set; }

        public long PlayTime { get; set; }
    }

    public record UserFilteringDTO(DateOnly? DateCreated, bool? DateBefore, short? Elo, bool? EloBigger, UserRoles? Role)
    {
    }


}
