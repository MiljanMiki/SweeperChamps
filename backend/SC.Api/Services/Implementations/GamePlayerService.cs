using SC.Api.Services.Interfaces;
using SC.Domain.DataModels;
using SC.Domain.DTOs.Service;
using SC.Domain.Repositories.AsyncInterfaces;

namespace SC.Api.Services.Implementations
{
    public class GamePlayerService : IGamePlayerService
    {
        private readonly IGamePlayerRepository _gamePlayerRepository;

        public GamePlayerService(IGamePlayerRepository gamePlayerRepository)
        {
            _gamePlayerRepository = gamePlayerRepository;
        }

        public async Task RecordPlayerResults(List<GameFinishedPlayerStatsDto> playerResults)
        {
            ArgumentNullException.ThrowIfNull(playerResults);

            if (playerResults.Count == 0)
                throw new ArgumentException("Empty player results dto");

            foreach (var result in playerResults)
            {
                var gp = await _gamePlayerRepository.GetAsync(result.GamePlayerId);
                if (gp == null)
                    throw new KeyNotFoundException($"Invalid {nameof(GamePlayer)} ID:{result.GamePlayerId}");

                gp.Score = result.Score;
                gp.Outcome = result.Outcome;
                if(result.EloChange.HasValue)
                    gp.EloChange = result.EloChange.Value;

                gp.Accuracy = result.Accuracy;
            }

            await _gamePlayerRepository.SaveChangesAsync();
        }
    }
}
