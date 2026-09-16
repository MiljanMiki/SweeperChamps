using SC.Messaging.Matchmaker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Matchmaker.Core
{
    public class TicketPool
    {
        // Outer Key: Pool ID (e.g., "SettingsId-IsRanked")
        // Inner Key: UserId (ensures O(1) lookups and prevents duplicate tickets for the same user)
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MatchTicketRequest>> _pools = new();

        // Secondary index for fast cancellations across all pools
        //UserId : poolKey
        private readonly ConcurrentDictionary<string, string> _userToPoolMap = new();

        public void AddTicket(MatchTicketRequest ticket)
        {
            var poolKey = GetPoolKey(ticket.GameSettingsId, ticket.IsRanked);

            var pool = _pools.GetOrAdd(poolKey, _ => new ConcurrentDictionary<string, MatchTicketRequest>());

            if (pool.TryAdd(ticket.UserId, ticket))
            {
                _userToPoolMap[ticket.UserId] = poolKey;
            }
        }

        public void RemoveTicket(string userId)
        {
            if (_userToPoolMap.TryRemove(userId, out var poolKey))
            {
                if (_pools.TryGetValue(poolKey, out var pool))
                {
                    pool.TryRemove(userId, out _);
                }
            }
        }

        public void RemoveTickets(IEnumerable<string> userIds)
        {
            foreach (var id in userIds)
            {
                RemoveTicket(id);
            }
        }

        public IEnumerable<MatchTicketRequest> GetTicketsInPool(int gameSettingsId, bool isRanked)
        {
            var poolKey = GetPoolKey(gameSettingsId, isRanked);
            if (_pools.TryGetValue(poolKey, out var pool))
            {
                return pool.Values;
            }
            return Enumerable.Empty<MatchTicketRequest>();
        }

        private static string GetPoolKey(int gameSettingsId, bool isRanked)
            => $"{gameSettingsId}-{(isRanked ? "Ranked" : "Casual")}";
    }
}
