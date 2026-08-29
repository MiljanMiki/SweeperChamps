using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SC.Domain.DataModels;

[Table("games")]
public partial class Game
{
    [Key]
    [Column("games_id")]
    public int GamesId { get; set; }

    [Column("start_time", TypeName = "timestamp without time zone")]
    public DateTime StartTime { get; set; }

    [Column("end_time", TypeName = "timestamp without time zone")]
    public DateTime? EndTime { get; set; }

    [Column("status", TypeName = "character varying")]
    public GameStatuses Status { get; set; }

    [Column("is_ranked")]
    public bool IsRanked { get; set; }

    [Column("duration_seconds")]
    public int? DurationSeconds { get; set; }

    [Column("winning_team")]
    public TeamColors? WinningTeam { get; set; }

    [InverseProperty("Game")]
    public virtual ICollection<GamePlayer> GamePlayers { get; set; } = new List<GamePlayer>();

    [Column("game_settings_id")]
    public int GameSettingsId { get; set; }

    [ForeignKey("GameSettingsId")]
    public virtual GameSetting GameSettings { get; set; } = null!;

    [InverseProperty("Game")]
    public virtual ICollection<Move> Moves { get; set; } = new List<Move>();
}


public enum GameStatuses
{ 
    Finished = 0,
    InProgress = 10,
    Aborted = 20, //korisnici su matchovani ali nije uspesno postavljena konekcija ili su odustali u prvih 1/2 poteza
    Terminated = 30//jedan od korisnika je banovan u toku partije i ona je nevazeca
}

