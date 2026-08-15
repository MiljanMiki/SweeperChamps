using SC_Backend.DataModels;

namespace SC_Backend.Repositories
{
    public interface IUserRepository : IAsyncRepository<User>
    {
        Task<User?> GetUserByUsername(string username);
        Task<User?> GetUserByEmail(string email);
        Task<User?> GetUserWithLoadedProperties(int id, bool history, bool stats);
        Task<bool> IsUniqueUsernameOrEmailAsync(string username, string email);
        Task<IEnumerable<User>> FilterUsersAsync(DateOnly? dateCreated, bool? before, short? elo, bool? bigger, UserRoles? role);
        Task<IEnumerable<User>> GetLeaderboardAsync(int topCount);

        //fja ne radi nikakvu proveru, vec samo brise sve NotSet korisnike. Predpodstavka da korisnik sa NotSet role ne moze
        //maltene nista da radi
        Task ClearNotSetRoles();
    }
}
