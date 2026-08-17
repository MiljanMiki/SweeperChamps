using SC_Backend.DataModels;

namespace SC_Backend.Repositories
{
    public interface IMovesRepository : IAsyncRepository<Move>
    {
        Task<Move?> GetByGameIdAsync(int gameId);
        Task DeleteByGameIdAsync(int gameId);
        Task<bool> HasMovesForGameAsync(int gameId);
    }
}
