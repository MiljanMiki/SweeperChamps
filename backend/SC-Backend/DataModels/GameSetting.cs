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
    public int? StartTimeSeconds { get; set; } //null for race mode
    
    [Column("team_size")]
    public int TeamSize { get; set; } // 1, 2, 3...

    [Column("win_condition")]
    public WinConditions WinCondition { get; set; } // "Race", "TimeRush"

    [Column("has_powerups")]
    public bool HasPowerUps { get; set; } // true (Custom), false (Classic)

    [InverseProperty("GameSettings")]
    public virtual Game Game{ get; set; } = null!;
}

public enum WinConditions
{
    Race,
    TimeRush
}
