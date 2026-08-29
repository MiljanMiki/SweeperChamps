using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SC.Api.Hubs.Interfaces;
using SC.Api.Services.Interfaces;
using SC.Messaging.Matchmaker;

namespace SC.Api.Hubs
{
    [Authorize] // Ensures Context.UserIdentifier is populated via JWT
    public class MatchmakingHub : Hub<IMatchmakingClient>
    {
        private readonly IMatchmakingPublisher _publisher;
        private readonly ILogger<MatchmakingHub> _logger;

        public MatchmakingHub(IMatchmakingPublisher publisher, ILogger<MatchmakingHub> logger)
        {
            _publisher = publisher;
            _logger = logger;
        }

        // This is the method the frontend calls via connection.invoke("RequestMatch", settingsId, true)
        public async Task RequestMatch(int gameSettingsId, bool isRanked)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.Error("Authentication failed.");
                return;
            }

            var ticket = new MatchTicketRequest
            {
                UserId = userId,
                GameSettingsId = gameSettingsId,
                IsRanked = isRanked,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                // 1. Send to RabbitMQ
                _publisher.PublishTicket(ticket);

                // 2. Notify the specific client that they are officially in the queue
                await Clients.Caller.MatchmakingStarted();

                _logger.LogInformation("User {UserId} queued for Settings: {SettingsId}", userId, gameSettingsId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish ticket for {UserId}", userId);
                await Clients.Caller.Error("Matchmaking service is currently unavailable.");
            }
        }

        // Handle unexpected disconnects (e.g., player closes app while in queue)
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                // Tell the background worker to drop this user's ticket
                _publisher.PublishCancelTicket(userId);
                _logger.LogInformation("User {UserId} disconnected. Ticket cancelled.", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
