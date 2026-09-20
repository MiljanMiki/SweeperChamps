using SC.Matchmaker.Strategies.Interfaces;
using SC.Messaging.Matchmaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Matchmaker.Strategies.Implementations
{
    public class EloMatchmakingStrategy : IMatchmakingStrategy
    {
        private readonly int _requiredPlayers;
        private readonly int _maxEloDifference;

        public EloMatchmakingStrategy(int requiredPlayers, int maxEloDifference = 100)
        {
            _requiredPlayers = requiredPlayers;
            _maxEloDifference = maxEloDifference;
        }

        public List<MatchTicketRequest>? TryMatch(IEnumerable<MatchTicketRequest> tickets)
        {
            var waitingList = tickets.Where(t => t.IsRanked == true && t.Elo.HasValue).OrderBy(t => t.Timestamp).ToList();

            foreach (var anchor in waitingList)
            {
                var potentialMatch = waitingList
                    .Where(t => Math.Abs(t.Elo!.Value - anchor.Elo!.Value) <= _maxEloDifference)
                    .Take(_requiredPlayers)
                    .ToList();

                if (potentialMatch.Count == _requiredPlayers)
                {
                    return potentialMatch;
                }
            }

            return null;
        }
    }
}
