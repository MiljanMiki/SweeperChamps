using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.GamePlayers;
using System.Threading.Tasks;

namespace SC_Backend.Repositories
{
    public class GamePlayerRepository : IGamePlayerRepository
    {
        private readonly ApplicationDbContext _context;

        public GamePlayerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        #region AsyncImpl
        public async Task<GamePlayer?> GetAsync(int id)
        {
            return await _context.GamePlayers.FindAsync(id);
        }

        public async Task<IEnumerable<GamePlayer>> GetAllAsync()
        {
            return await _context.GamePlayers.AsNoTracking().ToListAsync();
        }
        public void Add(GamePlayer entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var user = _context.Users.Find(entity.PlayerId);
            if (user == null)
                throw new KeyNotFoundException($"FK {entity.PlayerId} of {nameof(User)} does not map to any row.");

            var game = _context.Games.Find(entity.GameId);
            if (game == null)
                throw new KeyNotFoundException($"FK {entity.GameId} of {nameof(Game)} does not map to any row.");
            _context.GamePlayers.Add(entity);

        }

        public void Update( GamePlayer entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.GamePlayers.Update(entity);
        }
        public void Delete(GamePlayer entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.GamePlayers.Remove(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        #endregion AsyncImpl



        public async Task<IEnumerable<Game>> GamesBetweenPlayersAsync(int[] playerIDs)
        {
            ArgumentNullException.ThrowIfNull(playerIDs);

            var distinctPlayerIds = playerIDs.Distinct().ToArray();
            if (distinctPlayerIds.Length < 2)
                throw new ArgumentException("At least two distinct player IDs are required to find games between players.", nameof(playerIDs));

            int requiredPlayerCount = distinctPlayerIds.Length;

            //Find all GameIds where ALL provided players participated
            var sharedGameIds = _context.GamePlayers
                .Where(gp => distinctPlayerIds.Contains(gp.PlayerId))
                .GroupBy(gp => gp.GameId)
                .Where(group => group.Select(gp => gp.PlayerId).Distinct().Count() == requiredPlayerCount)
                .Select(group => group.Key);

            return await _context.Games
                .AsNoTracking()
                .Where(g => sharedGameIds.Contains(g.GamesId))
                .Include(g => g.GameSettings)
                .Include(g => g.GamePlayers)
                    .ThenInclude(gp => gp.Player)
                .ToListAsync();
        }

        public async Task<IEnumerable<Game>> GetGamesFromPlayerAsync(int playerID, bool orderByScore = false)
        {
            var query = _context.GamePlayers
                        .AsNoTracking()
                        .Include(player => player.Game)
                        .Where(player => player.PlayerId == playerID);

            if (orderByScore)
                query = query.OrderByDescending(game => game.Score);

            return await query.Select(player => player.Game).ToListAsync();
        }

        public async Task<IEnumerable<GamePlayer>> GetAllPlayersFromGameAsync(int gameId)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                throw new KeyNotFoundException($"FK {gameId} of {nameof(Game)} does not map to any row.");

            var listaIgraca = await _context.GamePlayers
                .AsNoTracking()
                .Include(player => player.Player)//moze da ide i dublje, do userstats pa tu da se izvlaci sta ocemo
                .Where(player => player.GameId == gameId)
                .ToListAsync();

            return listaIgraca;
        }

        public async Task<GamePlayer?> GetLoadedGamePlayerAsync(int id)
        {
            return await _context.GamePlayers.AsNoTracking().Include(g => g.Game).Include(g => g.Player).FirstOrDefaultAsync(g => g.GamePlayersId == id);
        }

        public async Task<IEnumerable<Game>> GetGamesFromPlayerWithSettingAsync(int playerID, int settingID)
        {
            return await _context.GamePlayers
                .AsNoTracking()
                .Where(g=>g.PlayerId == playerID && g.Game.GameSettingsId == settingID)
                .Select(g=>g.Game)
                .ToListAsync();
        }
    }
}
