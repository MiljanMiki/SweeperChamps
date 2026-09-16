using SC.Matchmaker.Strategies.Interfaces;
using SC.Messaging.Matchmaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Matchmaker.Strategies.Implementations
{
    public class OldestTicketStrategy : IMatchmakingStrategy
    {
        private readonly int _requiredPlayers;

        public OldestTicketStrategy(int requiredPlayers)
        {
            _requiredPlayers = requiredPlayers;
        }

        public List<MatchTicketRequest>? TryMatch(IEnumerable<MatchTicketRequest> tickets)
        {
            var waitingList = tickets.OrderBy(t => t.Timestamp).ToList();

            if (waitingList.Count >= _requiredPlayers)
            {
                return waitingList.Take(_requiredPlayers).ToList();
            }

            return null;
        }
    }
}
