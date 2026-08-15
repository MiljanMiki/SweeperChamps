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

        public async Task<bool> IsUniqueUsernameOrEmailAsync(string username, string email)
        {
            ArgumentNullException.ThrowIfNull(username);
            ArgumentNullException.ThrowIfNull(email);

            var list = await _context.Users.AsNoTracking().Where(user => user.Username == username || user.Email == email).ToListAsync();
            if (list.Count != 0)
                return false;
            return true;
        }

        public async Task<IEnumerable<User>> FilterUsersAsync(DateOnly? dateCreated,bool? before , short? elo, bool? bigger, UserRoles? role)
        {
            if (dateCreated == null && before == null && elo == null && bigger == null && role == null)
                throw new ArgumentNullException("All arguments are null. Atleast one must have a non null value.");

            var query = _context.Users.AsNoTracking();
            if (dateCreated.HasValue && before.HasValue)
                query = query.Where(user => before.Value ? user.Datecreated < dateCreated : user.Datecreated >= dateCreated);
            if(elo.HasValue && bigger.HasValue)
                query = query.Where(user => bigger.Value ? user.Elo < elo: user.Elo >= elo);
            if (role.HasValue)
                query = query.Where(user => user.UserRole == role);

            return await query.ToListAsync();
        }

        public async Task ClearNotSetRoles()
        {
            await _context.Users.Where(user => user.UserRole == UserRoles.NotSet).ExecuteDeleteAsync();
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            ArgumentNullException.ThrowIfNull(username);
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Username == username);
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            ArgumentNullException.ThrowIfNull(email);
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Email == email);
        }

        public async Task<IEnumerable<User>> GetLeaderboardAsync(int topCount)
        {
            return await _context.Users.AsNoTracking().Where(u=>u.Elo != null).OrderByDescending(user=> user.Elo).Take(topCount).ToListAsync();
        }

        public async Task<User?> GetUserWithLoadedProperties(int id, bool history, bool stats)
        {
            var query = _context.Users.AsNoTracking();
            
            if(history)
                query = query.Include(u => u.GamePlayers);
            if(stats)
                query = query.Include(u => u.UserStats);

            return await query.FirstOrDefaultAsync(u => u.UsersId == id);
        }
    }
}
