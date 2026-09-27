using SC.Domain.DataModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Domain.DTOs.Service
{
    public record MarkGameFinishedDto(int gameId, int durationSeconds, TeamColors winningTeam)
    {

    }
    public record GameFinishedPlayerStatsDto
    {
        public int GamePlayerId { get; set; }
        public int Score { get; set; }
        public Outcomes Outcome { get; set; }
        public short? EloChange { get; set; }
        public double Accuracy { get; set; }
    }

    public record UpdatePlayerStatsDto(int GameSettingId,
        int UserId, bool isRanked, bool isWin,long gameDuration)
    {
    }

    public record GameResultDto(MarkGameFinishedDto gameDto, List<GameFinishedPlayerStatsDto> playerDto,
                            List<UpdatePlayerStatsDto> statsDto)
    { }


}
