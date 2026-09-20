using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Matchmaker.Strategies.Interfaces
{
    public interface IMatchmakingStrategyFactory
    {
        IMatchmakingStrategy CreateStrategy(int requiredPlayers, bool isRanked);
    }
}
