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
    public string Status { get; set; } = null!;

    [InverseProperty("Game")]
    public virtual ICollection<GamePlayer> GamePlayers { get; set; } = new List<GamePlayer>();

    [InverseProperty("Game")]
    public virtual ICollection<GameSetting> GameSettings { get; set; } = new List<GameSetting>();

    [InverseProperty("Game")]
    public virtual ICollection<Move> Moves { get; set; } = new List<Move>();
}
