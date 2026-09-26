using SC.Matchmaker.Services.Interfaces;
using SC.Matchmaker.Strategies.Implementations;
using SC.Matchmaker.Strategies.Interfaces;
using SC.Messaging.Matchmaker;
using SC.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC.Api.Services.Interfaces;

namespace SC.Matchmaker.Core
{
    public class MatchmakingEngine
    {
        private readonly TicketPool _ticketPool;
        private readonly IMatchmakingStrategy _strategy;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMatchFoundPublisher _resultsPublisher;
        private readonly ILogger<MatchmakingEngine> _logger;

        public MatchmakingEngine(TicketPool ticketPool, IMatchmakingStrategy strategy,
            IServiceScopeFactory scopeFactory,
            IMatchFoundPublisher resultsPublisher, ILogger<MatchmakingEngine> logger)
        {
            _ticketPool = ticketPool;
            _resultsPublisher = resultsPublisher;
            _scopeFactory = scopeFactory;
            _strategy = strategy;
            _logger = logger;
        }

        public async Task ProcessNewTicket(MatchTicketRequest ticket)
        {
            _ticketPool.AddTicket(ticket);
            _logger.LogInformation("Added User {UserId} to pool.", ticket.UserId);

            await TryFormMatchAsync(ticket.GameSettingsId, ticket.IsRanked);
        }

        public void CancelTicket(string userId)
        {
            _ticketPool.RemoveTicket(userId);
            _logger.LogInformation("Removed User {UserId} from pool.", userId);
        }

        private async Task TryFormMatchAsync(int gameSettingsId, bool isRanked)
        {
            // Example: Dynamically select strategy based on GameSettingsId
            // In a real app, you might fetch game capacity from a database or config

            var availableTickets = _ticketPool.GetTicketsInPool(gameSettingsId, isRanked);
            if (!availableTickets.Any())
                return;

            int requiredPlayers = availableTickets.First().RequiredPlayers;


            var matchedLobby = _strategy.TryMatch(availableTickets);

            if (matchedLobby == null) return;

            try
            {
                var matchedUserIds = matchedLobby.Select(t => t.UserId).ToList();

                // Atomically remove players from the pool so they aren't matched twice
                _ticketPool.RemoveTickets(matchedUserIds);

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IGameService>();
                int newGameId = await service.CreateGame(gameSettingsId, matchedUserIds, isRanked);

                //Publish to RabbitMQ
                var matchEvent = new MatchFoundEvent { GameId = newGameId, UserIds = matchedUserIds };
                _resultsPublisher.Publish(matchEvent);

                _logger.LogInformation("MATCH FOUND! Players: {Players}", string.Join(", ", matchedUserIds));
            }
            catch(Exception e)
            {
                _logger.LogError(e,e.Message);
            }

        }
    }
}
