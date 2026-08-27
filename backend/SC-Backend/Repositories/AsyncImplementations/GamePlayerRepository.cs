using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.GamePlayers;
using SC_Backend.Repositories.AsyncInterfaces;
using System.Configuration;
using System.Threading.Tasks;

namespace SC_Backend.Repositories.AsyncImplementations
{
    public class GamePlayerRepository : BaseAsyncRepository<GamePlayer>, IGamePlayerRepository
    {
        public GamePlayerRepository(ApplicationDbContext context) : base(context) {}

        public override void Add(GamePlayer entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var user = Context.Users.Find(entity.PlayerId);
            if (user == null)
                throw new KeyNotFoundException($"FK {entity.PlayerId} of {nameof(User)} does not map to any row.");

            var game = Context.Games.Find(entity.GameId);
            if (game == null)
                throw new KeyNotFoundException($"FK {entity.GameId} of {nameof(Game)} does not map to any row.");
            DbSet.Add(entity);

        }
         public async Task<IEnumerable<Game>> GamesBetweenPlayersAsync(int[] playerIDs)
        {
            ArgumentNullException.ThrowIfNull(playerIDs);

            var distinctPlayerIds = playerIDs.Distinct().ToArray();
            if (distinctPlayerIds.Length < 2)
                throw new ArgumentException("At least two distinct player IDs are required to find games between players.", nameof(playerIDs));

            int requiredPlayerCount = distinctPlayerIds.Length;

            //Find all GameIds where ALL provided players participated
            var sharedGameIds = DbSet
                .Where(gp => distinctPlayerIds.Contains(gp.PlayerId))
                .GroupBy(gp => gp.GameId)
                .Where(group => group.Select(gp => gp.PlayerId).Distinct().Count() == requiredPlayerCount)
                .Select(group => group.Key);

            return await Context.Games
                .AsNoTracking()
                .Where(g => sharedGameIds.Contains(g.GamesId))
                .Include(g => g.GameSettings)
                .Include(g => g.GamePlayers)
                    .ThenInclude(gp => gp.Player)
                .ToListAsync();
        }

        public async Task<IEnumerable<Game>> GetGamesFromPlayerAsync(int playerID, bool orderByScore = false)
        {
            var query = DbSet
                        .AsNoTracking()
                        .Include(player => player.Game)
                        .Where(player => player.PlayerId == playerID);

            if (orderByScore)
                query = query.OrderByDescending(game => game.Score);

            return await query.Select(player => player.Game).ToListAsync();
        }

        public async Task<IEnumerable<GamePlayer>> GetAllPlayersFromGameAsync(int gameId)
        {
            var game = await Context.Games.FindAsync(gameId);
            if (game == null)
                throw new KeyNotFoundException($"FK {gameId} of {nameof(Game)} does not map to any row.");

            var listaIgraca = await DbSet
                .AsNoTracking()
                .Include(player => player.Player)//moze da ide i dublje, do userstats pa tu da se izvlaci sta ocemo
                .Where(player => player.GameId == gameId)
                .ToListAsync();

            return listaIgraca;
        }

        public async Task<GamePlayer?> GetLoadedGamePlayerAsync(int id)
        {
            return await DbSet
                .AsNoTracking()
                .Include(g => g.Game)
                .Include(g => g.Player)
                .FirstOrDefaultAsync(g => g.GamePlayersId == id);
        }

        public async Task<IEnumerable<Game>> GetGamesFromPlayerWithSettingAsync(int playerID, int settingID)
        {
            var games = await DbSet
                .AsNoTracking()
                .Where(g=>g.PlayerId == playerID && g.Game.GameSettingsId == settingID)
                .Select(g=>g.Game)
                .ToListAsync();

            if (games.Count == 0 && !await Context.GameSettings.AnyAsync(gs => gs.GameSettingsId == settingID) )
                throw new KeyNotFoundException($"{nameof(GameSetting)} does not exist with given IDs");

            return games;
        }

        public async Task<IEnumerable<GamePlayer>> GetUserMatchHistoryAsync(int userId, int page, int pageSize)
        {
            return await DbSet
                        .AsNoTracking()
                        .Include(gp => gp.Game)
                        .Where(gp => gp.PlayerId == userId && gp.Game.EndTime != null) // Only finished games
                        .OrderByDescending(gp => gp.Game.EndTime)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
        }
        public async Task UpdatePlayerResultsAsync(IEnumerable<GamePlayer> finalPlayerStats)
        {
            DbSet.UpdateRange(finalPlayerStats);
            await SaveChangesAsync();
        }
        public async Task<int> GetTotalScoreForUserAsync(int userId)
        {
            return await DbSet
                        .Where(gp => gp.PlayerId == userId)
                        .SumAsync(gp => gp.Score);
        }
    }
}
