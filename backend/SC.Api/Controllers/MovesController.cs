using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC.Domain.DataModels;
using SC.Domain.DTOs.Moves;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SC.Domain.Repositories.AsyncInterfaces;

namespace SC.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovesController : ControllerBase
    {
        private readonly  IMovesRepository _movesRepository;
        public MovesController(IMovesRepository repo)
        {
            _movesRepository = repo;
        }

        // GET: api/Moves
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<MoveDTO>>> GetMovesAsync()
        {
            var moves = await _movesRepository.GetAllAsync();
            return Ok(moves.Select(MapToDto).ToList());
        }

        // GET: api/Moves/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MoveDTO>> GetMoveAsync(int id)
        {
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0");

            var move = await _movesRepository.GetAsync(id);

            if (move == null)
            {
                return NotFound("Move with given id does not exist");
            }

            return Ok(MapToDto(move));
        }

        // PUT: api/Moves/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMoveAsync(int id, PutDTO dto)
        {
            if(id <= 0 )
                return BadRequest("ID cannot be negative or 0");

            if (dto == null)
                return BadRequest("DTO cannot be null");
            if (string.IsNullOrEmpty(dto.MoveLog))
                return BadRequest("MoveLog cannot be null or empty");

            var move = await _movesRepository.GetAsync(id);
            if (move == null)
                return NotFound($"Move with id {id} does not exist");

            move.MoveLog = dto.MoveLog;

            try
            {
                await _movesRepository.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await MoveExists(id) == false)
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

        // POST: api/Moves
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Move>> PostMoveAsync(MoveDTO dto)
        {
            if (dto.GameId <= 0)
                return BadRequest("FK to Game cannot be negative or 0");
            try
            {
                Move move = new Move
                {
                    GameId = dto.GameId,
                    MoveLog = dto.MoveLog
                };

                _movesRepository.Add(move);
                await _movesRepository.SaveChangesAsync();

                return CreatedAtAction("GetMove", new { id = move.MovesId }, move);
            }
            catch(ArgumentNullException e)
            {
                return BadRequest(e.Message);
            }
            catch(KeyNotFoundException e)
            {
                return NotFound(e.Message);
            }
            
        }

        // DELETE: api/Moves/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMoveAsync(int id)
        {
            if(id <=0)
            {
                return BadRequest("ID cannot be negative or 0");
            }
            var move = await _movesRepository.GetAsync(id);
            if (move == null)
            {
                return NotFound($"ID {id} does not exist in the database.");
            }

            try
            {
                _movesRepository.Delete(move);
                await _movesRepository.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            return NoContent();
        }

        [HttpGet("get-from-game/{gameId}")]
        public async Task<ActionResult<MoveDTO>> GetByGameIdAsync(int gameId)
        {
            if(gameId <=0)
                return BadRequest("ID cannot be negative or 0");
            var move = await _movesRepository.GetByGameIdAsync(gameId);
            if (move == null)
                return NotFound($"{nameof(Move)} not found from game ID {gameId}");
            return Ok(MapToDto(move));
        }

        [HttpDelete("delete-from-game/{gameId}")]
        public async Task<IActionResult> DeleteByGameIdAsync(int gameId)
        {
            if (gameId <= 0)
                return BadRequest("ID cannot be negative or 0");

            await _movesRepository.DeleteByGameIdAsync(gameId);

            return NoContent();
        }

        [HttpGet("has-moves/{gameId}")]
        public async Task<ActionResult<bool>> HasMovesForGameAsync(int gameId)
        {
            if (gameId <= 0)
                return BadRequest("ID cannot be negative or 0");

            return await _movesRepository.HasMovesForGameAsync(gameId);
        }


        private static MoveDTO MapToDto(Move move)
        {
            ArgumentNullException.ThrowIfNull(move);

            return new MoveDTO
            {
                GameId = move.GameId,
                MoveLog = move.MoveLog
            };
        }
        private async Task<bool> MoveExists(int id)
        {
            return await _movesRepository.GetAsync(id) != null;
        }
    }
}
