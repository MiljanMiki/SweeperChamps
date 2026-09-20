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
        public IMatchmakingStrategy CreateStrategy(int requiredPlayers, bool isRanked)
        {
            if (isRanked)
            {
                return new EloMatchmakingStrategy(requiredPlayers, maxEloDifference: 100);
            }
            return new OldestTicketStrategy(requiredPlayers);
        }
    }
}
