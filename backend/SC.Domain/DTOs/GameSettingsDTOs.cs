using SC.Domain.DataModels;

namespace SC.Domain.DTOs.GameSettings
{

    public record GameSettingDto
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public int NumberOfMines { get; set; }

        public int? StartTimeSeconds { get; set; } 

        public int TeamSize { get; set; } 

        public WinConditions WinCondition { get; set; } 

        public bool HasPowerUps { get; set; } 

    }

}
