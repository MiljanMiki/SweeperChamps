using SC.Matchmaker.Strategies.Implementations;
using SC.Matchmaker.Strategies.Interfaces;
using SC.Messaging.Matchmaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Matchmaker.Core
{
    public class MatchmakingEngine
    {
        private readonly TicketPool _ticketPool;
        private readonly ILogger<MatchmakingEngine> _logger;

        public MatchmakingEngine(TicketPool ticketPool, ILogger<MatchmakingEngine> logger)
        {
            _ticketPool = ticketPool;
            _logger = logger;
        }

        public void ProcessNewTicket(MatchTicketRequest ticket)
        {
            _ticketPool.AddTicket(ticket);
            _logger.LogInformation("Added User {UserId} to pool.", ticket.UserId);

            TryFormMatch(ticket.GameSettingsId, ticket.IsRanked);
        }

        public void CancelTicket(string userId)
        {
            _ticketPool.RemoveTicket(userId);
            _logger.LogInformation("Removed User {UserId} from pool.", userId);
        }

        private void TryFormMatch(int gameSettingsId, bool isRanked)
        {
            // Example: Dynamically select strategy based on GameSettingsId
            // In a real app, you might fetch game capacity from a database or config
            IMatchmakingStrategy strategy = gameSettingsId switch
            {
                1 => new OldestTicketStrategy(requiredPlayers: 2), // 1v1 Mode
                2 => new OldestTicketStrategy(requiredPlayers: 4), // 2v2 Mode
                _ => new OldestTicketStrategy(requiredPlayers: 2)
            };

            var availableTickets = _ticketPool.GetTicketsInPool(gameSettingsId, isRanked);

            var matchedLobby = strategy.TryMatch(availableTickets);

            if (matchedLobby != null)
            {
                var matchedUserIds = matchedLobby.Select(t => t.UserId).ToList();

                // Atomically remove players from the pool so they aren't matched twice
                _ticketPool.RemoveTickets(matchedUserIds);

                _logger.LogInformation("MATCH FOUND! Players: {Players}", string.Join(", ", matchedUserIds));

                // TODO in Phase 5: Publish MatchFoundEvent back to RabbitMQ for the API
            }
        }
    }
}
