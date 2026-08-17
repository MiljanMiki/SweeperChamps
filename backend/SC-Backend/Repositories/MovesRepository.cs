using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using System.Threading.Tasks;

namespace SC_Backend.Repositories
{
    public class MovesRepository : IMovesRepository
    {
        private readonly ApplicationDbContext _context;

        public MovesRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<Move?> GetAsync(int id)
        {
            return await _context.Moves.FindAsync(id);
        }

        public async Task<IEnumerable<Move>> GetAllAsync()
        {
            return await _context.Moves.AsNoTracking().ToListAsync();
        }
        public void Add(Move entity)
        {
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));

            var game = _context.Games.Find(entity.GameId);
            if (game == null)
                throw new KeyNotFoundException($"FK {entity.GameId} of {nameof(Game)} does not exist in the database");
            _context.Moves.Add(entity);
        }

        public void Delete(Move entity)
        {
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));
            _context.Moves.Remove(entity);
        }
        public void Update(Move entity)
        {
            ArgumentNullException.ThrowIfNull(entity, nameof(entity));
            _context.Moves.Update(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Move?> GetByGameIdAsync(int gameId)
        {
            return await _context.Moves.AsNoTracking().FirstOrDefaultAsync(m => m.GameId == gameId);
        }

        public async Task DeleteByGameIdAsync(int gameId)
        {
            await _context.Moves.Where(m => m.GameId == gameId).ExecuteDeleteAsync();
        }

        public async Task<bool> HasMovesForGameAsync(int gameId)
        {
            return await _context.Moves.AnyAsync(m => m.GameId == gameId);
        }
    }
}
