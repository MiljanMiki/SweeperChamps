using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.GamePlayers;
using SC_Backend.DTOs.Games;
using SC_Backend.Repositories.AsyncInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SC_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        private readonly IGameRepository _gameRepository;

        public GamesController(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        #region CRUD
        // GET: api/Games
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<GameDto>>> GetGamesAsync()
        {
            var listGames = await _gameRepository.GetAllAsync();

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

            var game = await _gameRepository.GetAsync(id);

            if (game == null)
            {
                return NotFound($"Game with id {id} does not exist");
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
            if (dto == null)
                return BadRequest("DTO is null");
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0.");

            var game = await _gameRepository.GetAsync(id);
            if (game == null)
                return BadRequest($"Game with ID {id} doesnt exist.");

            if (dto.EndTime < game.StartTime)
                return BadRequest("Invalid date: a game cannot end before it started.");
            if (dto.EndTime == null && (dto.Status == GameStatuses.Finished || dto.Status == GameStatuses.Terminated))
                return BadRequest("Invalid date: a game that has ended must have an end time.");
            if(dto.EndTime != null && (dto.Status == GameStatuses.Aborted || dto.Status == GameStatuses.InProgress))
                return BadRequest("A game that has not ended correctly cannot have end time.");
            if(!Enum.IsDefined(typeof(GameStatuses), dto.Status))
                    return BadRequest("Enum value is not defined");


            game.EndTime = dto.EndTime;
            game.Status = dto.Status;

            //_gameRepository.Update(game);

            try
            {
                await _gameRepository.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await GameExists(id) == false)
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
            if (dto == null)
                return BadRequest("DTO is null");
            if (dto.GameSettingsId <= 0)
                return BadRequest("ID cannot be negative or 0");
            if (dto.EndTime < dto.StartTime)
                return BadRequest("Game cannot end before it started.");

            if (dto.EndTime == null && dto.Status == GameStatuses.Finished || dto.Status == GameStatuses.Terminated)
                return BadRequest("Game that has ended must have an end time.");
            if (dto.EndTime != null && (dto.Status == GameStatuses.Aborted || dto.Status == GameStatuses.InProgress))
                return BadRequest("A game that has not ended correctly cannot have end time.");
            if (!Enum.IsDefined(typeof(GameStatuses), dto.Status))
                return BadRequest("Enum value is not defined");


            var game = new Game
            {
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = dto.Status,
                GameSettingsId = dto.GameSettingsId,
            };
            try
            {
                _gameRepository.Add(game);

                await _gameRepository.SaveChangesAsync();

                return CreatedAtAction(nameof(GetGameAsync), new { id = game.GamesId }, game);
            }
            catch(KeyNotFoundException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/Games/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGameAsync(int id)
        {
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0");

            var game = await _gameRepository.GetAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            try
            {
                _gameRepository.Delete(game);
                await _gameRepository.SaveChangesAsync();
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }

        #endregion CRUD

        
        /// 
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<GameDto>>> FilterGameByStatusAndDateAsync(GameStatuses status,DateTime? date = null,bool day=false,bool month=false,bool year=false)
        {
            try
            {
                var games = await _gameRepository.FilterGameByStatusAndDateAsync(status, date, day, month, year);

                return Ok(games.Select(g => new GameDto
                {
                    GamesId = g.GamesId,
                    StartTime = g.StartTime,
                    EndTime = g.EndTime,
                    Status = g.Status,
                    GameSettingsId = g.GameSettingsId
                }).ToList());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            
        }

        [HttpGet("duration/{durationSeconds}")]
        public async Task<ActionResult<IEnumerable<GameDto>>> FilterByDurationAsync(int durationSeconds, bool longer)
        {
            if (durationSeconds <= 0)
                return BadRequest("Game duration must be longer than 0 seconds");

            var games = await _gameRepository.FilterByDurationAsync(durationSeconds, longer);

            return games.Select(g => new GameDto
            {
                GamesId = g.GamesId,
                StartTime = g.StartTime,
                EndTime = g.EndTime,
                Status = g.Status,
                GameSettingsId = g.GameSettingsId
            }).ToList();
        }

        private async Task<bool> GameExists(int id)
        {
            return (await _gameRepository.GetAsync(id)) != null;
        }
    }
}
