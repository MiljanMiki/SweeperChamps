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
        public async Task<ActionResult<IEnumerable<GameDto>>> GetGamesAsync()
        {
            var listGames = await _context.Games.ToListAsync();

            return listGames.Select(game => new GameDto
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
        public async Task<ActionResult<GameDto>> GetGameAsync(int id)
        {
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0");

            var game = await _context.Games.FindAsync(id);

            if (game == null)
            {
                return NotFound();
            }

            return new GameDto {
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
                return BadRequest("Invalid date: a game cannot end before it started.");
            if (dto.EndTime == null && (dto.Status == GameStatuses.Finished || dto.Status == GameStatuses.Terminated))
                return BadRequest("Invalid date: a game that has ended must have an end time.");
            if(dto.EndTime != null && (dto.Status == GameStatuses.Aborted || dto.Status == GameStatuses.InProgress))
                return BadRequest("A game that has not ended correctly cannot have end time.");


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

            if (dto.EndTime == null && dto.Status == GameStatuses.Finished && dto.Status == GameStatuses.Terminated)
                return BadRequest("Game that has ended must have an end time.");
            if (dto.EndTime != null && (dto.Status == GameStatuses.Aborted || dto.Status == GameStatuses.InProgress))
                return BadRequest("A game that has not ended correctly cannot have end time.");

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

            return CreatedAtAction(nameof(GetGameAsync), new { id = game.GamesId }, game);
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

        ///  <summary>
        /// Filters games based on status and the date of the start of the game. If date is omitted, it will be filtered only by status.
        /// If date is not omitted then one of day,month or year parameters must be set to true or BadRequest is returned. Only the first parameter set 
        /// to true is considered in the query.
        /// </summary>
        /// <param name="status">Current status of the game.</param>
        /// <param name="date">Date by which the games will be filtered. If none of the following parameters is not set to true the query returns BadRequest: day, month, year</param>
        /// <param name="day">If set to true games will be filtered by day only. Month and year will not be considered</param>
        /// <param name="month">If set to true games will be filtered by month only.Day and year will not be considered</param>
        /// <param name="year">If set to true games will be filtered by year only. Day and month will not be considered</param>
        /// <returns>All games that satisfy the criteria.</returns>
        /// 
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<GameDto>>> FilterGameByStatusAndDateAsync(GameStatuses status,DateTime? date = null,bool day=false,bool month=false,bool year=false)
        {
            var query = _context.Games.Where(g => g.Status == status);

            if(date != null)
            {
                if (day)
                    query = query.Where(g => g.StartTime.Date == date.Value.Date);
                else if (month)
                    query = query.Where(g => g.StartTime.Month == date.Value.Month);
                else if (year)
                    query = query.Where(g => g.StartTime.Year == date.Value.Year);
                else
                    return BadRequest("Day, month or year must be specified for date filtering.");
            }

            return await query.Select(g => new GameDto
            {
                GamesId = g.GamesId,
                StartTime = g.StartTime,
                EndTime = g.EndTime,
                Status = g.Status,
                GameSettingsId = g.GameSettingsId
            }).ToListAsync();
        }

        [HttpGet("duration/{durationSeconds}")]
        public async Task<ActionResult<IEnumerable<GameDto>>> FilterByDurationAsync(int durationSeconds, bool longer)
        {
            if (durationSeconds <= 0)
                return BadRequest("Game duration must be longer than 0 seconds");

            var query = _context.Games.Where(g => g.Status == GameStatuses.Finished && g.EndTime != null);//za svaki slucaj i null check
            query = longer ?
                query.Where(g => EF.Functions.DateDiffSecond(g.StartTime, g.EndTime) > durationSeconds) :
                query.Where(g => EF.Functions.DateDiffSecond(g.StartTime, g.EndTime) <= durationSeconds);

            return await query.Select(g => new GameDto
            {
                GamesId = g.GamesId,
                StartTime = g.StartTime,
                EndTime = g.EndTime,
                Status = g.Status,
                GameSettingsId = g.GameSettingsId
            }).ToListAsync();
        }

        private bool GameExists(int id)
        {
            return _context.Games.Any(e => e.GamesId == id);
        }
    }
}
