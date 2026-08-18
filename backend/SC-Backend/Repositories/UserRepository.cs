using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;

namespace SC_Backend.Repositories
{
    public class UserRepository :BaseAsyncRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context){}
        public async Task<bool> IsUniqueUsernameOrEmailAsync(string username, string email)
        {
            ArgumentNullException.ThrowIfNull(username);
            ArgumentNullException.ThrowIfNull(email);

            return !await DbSet.AnyAsync(user => user.Username == username || user.Email == email);
        }
        public async Task<IEnumerable<User>> FilterUsersAsync(DateOnly? dateCreated,bool? before , short? elo, bool? smaller, UserRoles? role)
        {
            if (dateCreated == null && before == null && elo == null && smaller == null && role == null)
                throw new ArgumentNullException("All arguments are null. Atleast one must have a non null value.");

            var query = DbSet.AsNoTracking();
            if (dateCreated.HasValue)
            {
                if (before.HasValue)
                    query = query.Where(user => before.Value ? user.Datecreated < dateCreated : user.Datecreated >= dateCreated);
                else
                    query = query.Where(user => user.Datecreated == dateCreated);
            }
            if (elo.HasValue)
            {
                if(smaller.HasValue)
                    query = query.Where(user => smaller.Value ? user.Elo < elo : user.Elo >= elo);
                else
                    query = query.Where(user => user.Elo == elo);
            }
            if (role.HasValue)
                query = query.Where(user => user.UserRole == role);

            return await query.ToListAsync();
        }

        public async Task ClearNotSetRolesAsync()
        {
            await DbSet.Where(user => user.UserRole == UserRoles.NotSet).ExecuteDeleteAsync();
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            ArgumentNullException.ThrowIfNull(username);
            return await DbSet.AsNoTracking().FirstOrDefaultAsync(user => user.Username == username);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            ArgumentNullException.ThrowIfNull(email);
            return await DbSet.AsNoTracking().FirstOrDefaultAsync(user => user.Email == email);
        }

        public async Task<IEnumerable<User>> GetLeaderboardAsync(int topCount)
        {
            return await DbSet.AsNoTracking().Where(u=>u.Elo != null).OrderByDescending(user=> user.Elo).Take(topCount).ToListAsync();
        }

        public async Task<User?> GetUserWithLoadedPropertiesAsync(int id, bool history, bool stats)
        {
            var query = DbSet.AsNoTracking();
            
            if(history)
                query = query.Include(u => u.GamePlayers);
            if(stats)
                query = query.Include(u => u.UserStats);

            return await query.FirstOrDefaultAsync(u => u.UsersId == id);
        }

        public async Task<bool> AnyUserExists()
        {
            return await DbSet.AnyAsync();
        }
    }
}
