using SC_Backend.DataModels;

namespace SC_Backend.Repositories
{
    public interface IUserRepository : IAsyncRepository<User>
    {
        Task<User?> GetUserByUsername(string username);
        Task<User?> GetUserByEmail(string email);
        Task<User?> GetUserWithLoadedProperties(int id, bool history, bool stats);
        Task<bool> IsUniqueUsernameOrEmailAsync(string username, string email);

        ///  <summary>
        /// Filters users based on date created, elo and role. If date/elo is passed but no 
        /// filtering direction (before/after or bigger/smaller) then the query will filter for equality on the given
        /// parameters.
        /// 
        /// </summary>
        /// <returns>All users that satisfy the criteria.</returns>
        Task<IEnumerable<User>> FilterUsersAsync(DateOnly? dateCreated, bool? before, short? elo, bool? smaller, UserRoles? role);
        Task<IEnumerable<User>> GetLeaderboardAsync(int topCount);

        //fja ne radi nikakvu proveru, vec samo brise sve NotSet korisnike. Predpodstavka da korisnik sa NotSet role ne moze
        //maltene nista da radi
        Task ClearNotSetRoles();
    }
}
