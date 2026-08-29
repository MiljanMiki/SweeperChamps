using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC.Domain.DataModels;
using SC.Domain.Repositories.AsyncInterfaces;
using System.Threading.Tasks;

namespace SC_Backend.Repositories.AsyncImplementations
{
    public class MovesRepository :BaseAsyncRepository<Move>, IMovesRepository
    {
        public MovesRepository(ApplicationDbContext context) :base(context){}
        
        public override void Add(Move entity)
        {
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));

            var game = Context.Games.Find(entity.GameId);
            if (game == null)
                throw new KeyNotFoundException($"FK {entity.GameId} of {nameof(Game)} does not exist in the database");
            DbSet.Add(entity);
        }

        public async Task<Move?> GetByGameIdAsync(int gameId)
        {
            return await DbSet.AsNoTracking().FirstOrDefaultAsync(m => m.GameId == gameId);
        }

        public async Task DeleteByGameIdAsync(int gameId)
        {
            await DbSet.Where(m => m.GameId == gameId).ExecuteDeleteAsync();
        }

        public async Task<bool> HasMovesForGameAsync(int gameId)
        {
            return await DbSet.AnyAsync(m => m.GameId == gameId);
        }

        public async Task<Move?> GetLoadedGameAsync(int gameId)
        {
            return await DbSet.Include(m => m.Game).FirstOrDefaultAsync(m => m.GameId == gameId);
        }
    }
}
