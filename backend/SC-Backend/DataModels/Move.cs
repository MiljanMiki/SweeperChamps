using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SC_Backend.DataModels;

[Table("moves")]
public partial class Move
{
    [Key]
    [Column("moves_id")]
    public int MovesId { get; set; }

    [Column("game_id")]
    public int GameId { get; set; }

    [Column("move_log", TypeName = "jsonb")]
    public string MoveLog { get; set; } = null!;

    [ForeignKey("GameId")]
    [InverseProperty("Moves")]
    public virtual Game Game { get; set; } = null!;
}
