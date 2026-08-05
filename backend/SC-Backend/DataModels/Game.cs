using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SC_Backend.DataModels;

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
    Aborted = 10, //korisnici su matchovani ali nije uspesno postavljena konekcija ili su odustali u prvih 1/2 poteza
    Terminated = 20,//jedan od korisnika je banovan u toku partije i ona je nevazeca
    InProgress = 30
}

