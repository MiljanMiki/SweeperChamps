using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC.Api.Services;
using SC_Backend.DataContext;

namespace SC.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IGameServerClient _gameServerClient;

        public AdminController(IGameServerClient gameServerClient)
        {
            _gameServerClient = gameServerClient;
        }

        /// <summary>
        /// GET /api/Admin/active-games
        /// Returns all games currently running on the GameServer,
        /// enriched with usernames from the database.
        /// </summary>
        [HttpGet("active-games")]
        public async Task<IActionResult> GetActiveGames()
        {
            var games = await _gameServerClient.GetAllActiveGamesAsync();
            if (games.Count == 0) return Ok(new List<object>());

            var db = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

            var allPlayerIds = games
                .SelectMany(g => g.Players.Select(p => p.PlayerId))
                .Distinct()
                .ToList();

            var usernames = await db.Users
                .Where(u => allPlayerIds.Contains(u.UsersId))
                .ToDictionaryAsync(u => u.UsersId, u => u.Username);

            var result = games.Select(g => new
            {
                gameId = g.GameId,
                playerCount = g.PlayerCount,
                players = g.Players.Select(p => new
                {
                    playerId = p.PlayerId,
                    username = usernames.TryGetValue(p.PlayerId, out var name)
                               ? name
                               : $"Player {p.PlayerId}",
                    teamColor = p.TeamColor
                })
            });

            return Ok(result);
        }
    }
}