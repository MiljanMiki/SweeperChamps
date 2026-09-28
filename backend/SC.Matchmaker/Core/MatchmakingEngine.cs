using SC.Matchmaker.Services.Interfaces;
using SC.Matchmaker.Strategies.Interfaces;
using SC.Messaging.Matchmaker;
using SC.Messaging;
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

        public MatchmakingEngine(
            TicketPool ticketPool,
            IMatchmakingStrategy strategy,
            IServiceScopeFactory scopeFactory,
            IMatchFoundPublisher resultsPublisher,
            ILogger<MatchmakingEngine> logger)
        {
            _ticketPool = ticketPool;
            _resultsPublisher = resultsPublisher;
            _scopeFactory = scopeFactory;
            _strategy = strategy;
            _logger = logger;
        }

        public async Task ProcessNewTicket(MatchTicketRequest ticket)
        {
            if (ticket is null) return;
            if (string.IsNullOrWhiteSpace(ticket.UserId))
            {
                _logger.LogWarning("Ticket with null/empty UserId ignored. Settings={SettingsId}", ticket.GameSettingsId);
                return;
            }
            if (ticket.GameSettingsId <= 0)
            {
                _logger.LogWarning("Ticket with invalid GameSettingsId={Id} for User {UserId} ignored.", ticket.GameSettingsId, ticket.UserId);
                return;
            }

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
            var availableTickets = _ticketPool.GetTicketsInPool(gameSettingsId, isRanked).ToList();
            if (!availableTickets.Any()) return;

            var matchedLobby = _strategy.TryMatch(availableTickets);
            if (matchedLobby == null) return;

            try
            {
                var matchedUserIds = matchedLobby.Select(t => t.UserId).ToList();
                _ticketPool.RemoveTickets(matchedUserIds);

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IGameService>();
                int newGameId = await service.CreateGame(gameSettingsId, matchedUserIds, isRanked);

                var matchEvent = new MatchFoundEvent
                {
                    GameId = newGameId,
                    UserIds = matchedUserIds
                };
                _resultsPublisher.Publish(matchEvent);

                _logger.LogInformation("MATCH FOUND! GameId={GameId}, Players: {Players}",
                    newGameId, string.Join(", ", matchedUserIds));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error forming match for settings {SettingsId}", gameSettingsId);
            }
        }
    }
}