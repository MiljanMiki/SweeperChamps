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
    public TeamColors TeamColor { get; set; }

    [Column("score")]
    public int Score { get; set; }

    [Column("outcome")]
    public Outcomes Outcome { get; set; }

    [Column("elo_change")]
    public short? EloChange { get; set; }

    [Column("accuracy")]
    public double Accuracy { get; set; }


    [ForeignKey("GameId")]
    [InverseProperty("GamePlayers")]
    public virtual Game Game { get; set; } = null!;

    [ForeignKey("PlayerId")]
    [InverseProperty("GamePlayers")]
    public virtual User Player { get; set; } = null!;
}

public enum TeamColors
{ 
    Red,
    Blue
}
public enum Outcomes
{
    Win,
    Loss,
    Draw,
    Abandoned,
    Disconnected
}