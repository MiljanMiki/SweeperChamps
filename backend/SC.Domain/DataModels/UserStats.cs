using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SC.Domain.DataModels;

[Table("user_stats")]
public partial class UserStats
{

    [Column("game_setting_id")]
    public int GameSettingId{ get; set; }

    [ForeignKey("GameSettingId")]
    public virtual GameSetting GameSetting { get; set; } = null!;

    [Column("user_id")]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserStats")]
    public virtual User User { get; set; } = null!;

    [Column("is_ranked")]
    public bool IsRanked { get; set; }

    [Column("games_played")]
    public int GamesPlayed { get; set; }

    [Column("wins")]
    public int Wins { get; set; }

    [Column("losses")]
    public int Losses{ get; set; }

    [Column("playtime")]
    public  long  PlayTime { get; set; }

}

