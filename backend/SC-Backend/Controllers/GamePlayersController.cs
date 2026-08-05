using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.GamePlayers;

namespace SC_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GamePlayersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GamePlayersController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region CRUD
        // GET: api/GamePlayers
        //PROMENI U DTO
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GamePlayer>>> GetGamePlayersAsync()
        {
            return await _context.GamePlayers.ToListAsync();
        }

        // GET: api/GamePlayers/5
        //PROMENI U DTO
        [HttpGet("{id}")]
        public async Task<ActionResult<GamePlayer>> GetGamePlayerAsync(int id)
        {
            var gamePlayer = await _context.GamePlayers.FindAsync(id);

            if (gamePlayer == null)
            {
                return NotFound();
            }

            return gamePlayer;
        }

        // PUT: api/GamePlayers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGamePlayerAsync(int id, PutGamePlayerRequestDto gamePlayerDto)
        {
            var gamePlayer = await _context.GamePlayers.FindAsync(id);

            if (gamePlayer == null)
            {
                return BadRequest();
            }

            _context.Entry(gamePlayerDto).State = EntityState.Modified;

            gamePlayer.Score = gamePlayerDto.Score;
            gamePlayer.TeamColor = gamePlayerDto.TeamColor;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GamePlayerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/GamePlayers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<GamePlayer>> PostGamePlayerAsync(PostGamePlayerRequestDto gamePlayerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);


            var user = await _context.Users.FindAsync(gamePlayerDto.PlayerId);
            if (user == null)
                return BadRequest("User sa datim id-jem ne postoji");

            var game = await _context.Games.FindAsync(gamePlayerDto.GameId);
            if(game==null)
                return BadRequest("Game sa datim id-jem ne postoji");

            var gamePlayer = new GamePlayer
            {
                GameId = gamePlayerDto.GameId,
                PlayerId = gamePlayerDto.PlayerId,
                TeamColor = gamePlayerDto.TeamColor,
                Score = gamePlayerDto.Score
            };

            _context.GamePlayers.Add(gamePlayer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGamePlayerAsync), new { id = gamePlayer.GamePlayersId }, gamePlayer);
        }

        // DELETE: api/GamePlayers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGamePlayerAsync(int id)
        {
            var gamePlayer = await _context.GamePlayers.FindAsync(id);
            if (gamePlayer == null)
            {
                return NotFound();
            }

            _context.GamePlayers.Remove(gamePlayer);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion CRUD

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlayerSummaryDto>>> GetAllPlayersFromGame(int gameId)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                return BadRequest("Game sa zadatim id-jem ne postoji");

            var listaIgraca = await _context.GamePlayers
                .Include(player => player.Player)//moze da i ide i dublje, do userstats pa tu da se izvlaci sta ocemo
                .Where(player => player.GameId == gameId)
                .Select(player => new PlayerSummaryDto
                {
                    PlayerId = player.PlayerId,
                    Username = player.Player.Username,
                    TeamColor = player.TeamColor.ToString(),
                    Score = player.Score,
                    Elo = player.Player.Elo,
                })
                .ToListAsync();

            if (listaIgraca.Count == 0)
                return BadRequest("Vraceno 0 igraca");

            if (listaIgraca.Count % 2 != 0)
                return BadRequest("Broj vracenih igraca mora biti paran");


            return listaIgraca;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GameSummaryDto>>> GetAllGamesFromPlayer(int playerId, bool orderByScore = false)
        {
            var player = await _context.Users.FindAsync(playerId);
            if (player == null)
                return BadRequest("Igrac sa zadatim id-jem ne postoji");

            var query = _context.GamePlayers
                        .Include(player => player.Game)
                        .Where(player => player.PlayerId == playerId)
                        .Select(player => new GameSummaryDto
                        {
                            GamesId = player.Game.GamesId,
                            StartTime = player.Game.StartTime,
                            EndTime = player.Game.EndTime,
                            Status = player.Game.Status,
                            Score = player.Score
                        });

            if (orderByScore)
                query = query.OrderByDescending(game => game.Score);

            return await query.ToListAsync();
        }

        public async Task<ActionResult<IEnumerable<AllGamesTwoPlayersRequestDto>>> GamesBetweenTwoPlayers(int pId1, int pId2)
        {
            if (pId1 == pId2)
            {
                return BadRequest("A player cannot play against themselves.");
            }

            var player1GameIds = _context.GamePlayers
                .Where(gp => gp.PlayerId == pId1)
                .Select(gp => gp.GameId);

            var sharedGameIds = _context.GamePlayers
                .Where(gp => gp.PlayerId == pId2 && player1GameIds.Contains(gp.GameId))
                .Select(gp => gp.GameId);


            var games = await _context.Games
                .Where(g => sharedGameIds.Contains(g.GamesId))
                .Include(g => g.GameSettings)
                .Include(g => g.GamePlayers)
                    .ThenInclude(gp => gp.Player)
                .Select(g => new AllGamesTwoPlayersRequestDto
                {
                    GamesId = g.GamesId,
                    StartTime = g.StartTime,
                    EndTime = g.EndTime,
                    Status = g.Status,
                    Width = g.GameSettings.Width,
                    Height = g.GameSettings.Height,
                    NumberOfMines = g.GameSettings.NumberOfMines,
                    PlayerSummary = g.GamePlayers.Select(player => new PlayerSummaryDto
                    {
                        PlayerId = player.PlayerId,
                        Username = player.Player.Username,
                        TeamColor = player.TeamColor.ToString(),
                        Score = player.Score,
                        Elo = player.Player.Elo
                    }).ToList()
                })
                .ToListAsync();

            return Ok(games);
        }
        private bool GamePlayerExists(int id)
        {
            return _context.GamePlayers.Any(e => e.GamePlayersId == id);
        }
    }
}
