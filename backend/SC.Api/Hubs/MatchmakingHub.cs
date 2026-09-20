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
        private readonly IMatchmakingService _matchmakingService;
        private readonly ILogger<MatchmakingHub> _logger;

        public MatchmakingHub(IMatchmakingService service, ILogger<MatchmakingHub> logger)
        {
            _matchmakingService = service;
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


            try
            {
                // 1. Send to RabbitMQ
                await _matchmakingService.QueueUserAsync(userId, gameSettingsId, isRanked);

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
                _matchmakingService.PublishCancelTicket(userId);
                _logger.LogInformation("User {UserId} disconnected. Ticket cancelled.", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
