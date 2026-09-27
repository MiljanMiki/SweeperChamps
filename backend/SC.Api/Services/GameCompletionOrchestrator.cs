using Microsoft.EntityFrameworkCore;
using SC.Api.Services.Interfaces;
using SC.Domain.DTOs.Service;

namespace SC.Api.Services
{
    public interface IGameCompletionOrchestrator
    {
        Task FinishGameAsync(GameResultDto dto);
    }

    public class GameCompletionOrchestrator : IGameCompletionOrchestrator
    {
        private readonly IGameService _gameService;
        private readonly IGamePlayerService _playerService;
        private readonly IUserStatsService _userStatsService;
        private readonly DbContext _dbContext;

        public GameCompletionOrchestrator(IGameService gameService,
                                        IGamePlayerService gamePlayerService,
                                        IUserStatsService userStatsService,
                                        DbContext dbContext)
        {
            _gameService = gameService;
            _playerService = gamePlayerService;
            _userStatsService = userStatsService;
            _dbContext = dbContext;
        }

        public async Task FinishGameAsync(GameResultDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(dto.gameDto);
            ArgumentNullException.ThrowIfNull(dto.playerDto);
            if (dto.playerDto.Count == 0)
                throw new ArgumentException($"No user updates for {nameof(dto.playerDto)}");
            ArgumentNullException.ThrowIfNull(dto.statsDto);
            if (dto.statsDto.Count == 0)
                throw new ArgumentException($"No stats updates for {nameof(dto.statsDto)}");

            var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                await _gameService.MarkGameFinished(dto.gameDto);

                await _playerService.RecordPlayerResults(dto.playerDto);

                await _userStatsService.UpdatePlayerStats(dto.statsDto);


                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
