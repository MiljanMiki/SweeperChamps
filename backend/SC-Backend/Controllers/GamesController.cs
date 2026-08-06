using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.Games;

namespace SC_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GamesController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region CRUD
        // GET: api/Games
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetGameDto>>> GetGamesAsync()
        {
            var listGames = await _context.Games.ToListAsync();

            return listGames.Select(game => new GetGameDto
            {
                GamesId = game.GamesId,
                StartTime = game.StartTime,
                EndTime = game.EndTime,
                Status = game.Status,
                GameSettingsId = game.GameSettingsId
            }).ToList();
        }

        // GET: api/Games/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GetGameDto>> GetGameAsync(int id)
        {
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0");

            var game = await _context.Games.FindAsync(id);

            if (game == null)
            {
                return NotFound();
            }

            return new GetGameDto {
                GamesId = game.GamesId,
                StartTime = game.StartTime, 
                EndTime = game.EndTime,
                Status = game.Status,
                GameSettingsId = game.GameSettingsId
            };
        }

        // PUT: api/Games/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGameAsync(int id,PutGameDto dto)
        {
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0.");

            var game = await _context.Games.FindAsync(id);
            if (game == null)
                return BadRequest($"Game with ID {id} doesnt exist.");

            if (dto.EndTime < game.StartTime)
                return BadRequest("Game cannot end before it started.");

            _context.Entry(game).State = EntityState.Modified;

            game.EndTime = dto.EndTime;
            game.Status = dto.Status;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GameExists(id))
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

        // POST: api/Games
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Game>> PostGameAsync(PostGameDto dto)
        {
            if (dto.GameSettingsId <= 0)
                return BadRequest("ID cannot be negative or 0");
            if (dto.EndTime < dto.StartTime)
                return BadRequest("Game cannot end before it started.");

            var settings = await _context.GameSettings.FindAsync(dto.GameSettingsId);
            if (settings == null)
                return BadRequest("GameSettings sa datim id-jem ne postoji");

            var game = new Game
            {
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = dto.Status,
                GameSettingsId = dto.GameSettingsId,
            };
            _context.Games.Add(game);

            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGameAsync", new { id = game.GamesId }, game);
        }

        // DELETE: api/Games/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGameAsync(int id)
        {
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0");

            var game = await _context.Games.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            _context.Games.Remove(game);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion CRUD

        private bool GameExists(int id)
        {
            return _context.Games.Any(e => e.GamesId == id);
        }
    }
}
