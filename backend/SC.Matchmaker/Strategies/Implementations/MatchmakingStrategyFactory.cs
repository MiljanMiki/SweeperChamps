using SC.Matchmaker.Strategies.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Matchmaker.Strategies.Implementations
{
    public class MatchmakingStrategyFactory : IMatchmakingStrategyFactory
    {
        public IMatchmakingStrategy CreateStrategy(int gameSettingsId, bool isRanked)
        {
            int requiredPlayers = gameSettingsId switch
            {
                1 => 2, // e.g., 1v1
                2 => 4, // e.g., 2v2
                _ => 2
            };

            if (isRanked)
            {
                return new EloMatchmakingStrategy(requiredPlayers, maxEloDifference: 100);
            }

            return new OldestTicketStrategy(requiredPlayers);
        }
    }
}
