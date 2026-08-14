using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;

namespace SC_Backend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<User?> GetAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.AsNoTracking().ToListAsync();
        }
        public void Add(User entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.Users.Add(entity);
        }

        public void Delete(User entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.Users.Remove(entity);
        }
        public void Update(User entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.Users.Update(entity);
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        
    }
}
