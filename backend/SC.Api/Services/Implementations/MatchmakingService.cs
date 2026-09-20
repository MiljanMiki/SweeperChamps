using SC.Api.Services.Interfaces;
using SC.Domain.DataModels;
using SC.Domain.Repositories.AsyncInterfaces;
using SC.Messaging.Matchmaker;

namespace SC.Api.Services.Implementations
{
    public class MatchmakingService : IMatchmakingService
    {
        private readonly IUserRepository _userRepository;
        private readonly IGameSettingsService _gameSettingsService;
        private readonly IMatchmakingPublisher _publisher;

        public MatchmakingService(
            IUserRepository userRepository,
            IGameSettingsService gameSettingsService,
            IMatchmakingPublisher publisher)
        {
            _userRepository = userRepository;
            _gameSettingsService = gameSettingsService;
            _publisher = publisher;
        }
        public async Task QueueUserAsync(string userId, int gameSettingsId, bool isRanked)
        {
            // 1. Fetch User to retrieve Elo
            var user = await _userRepository.GetAsync(Int32.Parse(userId));
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            var requiredPlayers = await _gameSettingsService.GetRequiredPlayersAsync(gameSettingsId);

            // 2. Create the ticket contract with the user's Elo
            var ticket = new MatchTicketRequest {
                UserId = userId,
                GameSettingsId = gameSettingsId,
                IsRanked = isRanked,
                RequiredPlayers = requiredPlayers,
                Elo = user.Elo,
                Timestamp = DateTime.UtcNow
            };

            // 3. Publish to RabbitMQ
            _publisher.PublishTicket(ticket);
        }

        public void PublishCancelTicket(string userId)
        {

            _publisher.PublishCancelTicket(userId);
        }

        
    }
}
