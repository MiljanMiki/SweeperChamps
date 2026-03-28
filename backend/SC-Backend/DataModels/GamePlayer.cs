using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SC_Backend.DataModels;

[Table("game_players")]
public partial class GamePlayer
{
    [Key]
    [Column("game_players_id")]
    public int GamePlayersId { get; set; }

    [Column("game_id")]
    public int GameId { get; set; }

    [Column("player_id")]
    public int PlayerId { get; set; }

    [Column("team_color", TypeName = "character varying")]
    public string TeamColor { get; set; } = null!;

    [Column("score")]
    public int Score { get; set; }

    [ForeignKey("GameId")]
    [InverseProperty("GamePlayers")]
    public virtual Game Game { get; set; } = null!;

    [ForeignKey("PlayerId")]
    [InverseProperty("GamePlayers")]
    public virtual User Player { get; set; } = null!;
}
