using SC.Domain.DataModels;

namespace SC.Domain.Repositories.AsyncInterfaces
{
    public interface IMovesRepository : IAsyncRepository<Move>
    {
        Task<Move?> GetByGameIdAsync(int gameId);
        Task DeleteByGameIdAsync(int gameId);
        Task<bool> HasMovesForGameAsync(int gameId);

        Task<Move?> GetLoadedGameAsync(int gameId);
    }
}
