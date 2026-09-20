using SC.Messaging.Matchmaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Matchmaker.Strategies.Interfaces
{
    public interface IMatchmakingStrategy
    {
        /// <summary>
        /// Evaluates a pool of tickets and returns a matched group if successful, or null if no match can be formed.
        /// </summary>
        List<MatchTicketRequest>? TryMatch(IEnumerable<MatchTicketRequest> tickets);
    }
}
