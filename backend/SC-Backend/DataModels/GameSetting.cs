using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SC_Backend.DataModels;

[Table("game_settings")]
public partial class GameSetting
{
    [Key]
    [Column("game_settings_id")]
    public int GameSettingsId { get; set; }

    [Column("width")]
    public int Width { get; set; }

    [Column("height")]
    public int Height { get; set; }

    [Column("number_of_mines")]
    public int NumberOfMines { get; set; }

    [Column("start_time_seconds")]
    public int StartTimeSeconds { get; set; }

    [Column("time_format", TypeName = "character varying")]
    public string TimeFormat { get; set; } = null!;

    [Column("game_id")]
    public int GameId { get; set; }

    [ForeignKey("GameId")]
    [InverseProperty("GameSettings")]
    public virtual Game Game { get; set; } = null!;
}
