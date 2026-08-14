using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.Repositories;
using SC_Backend.DTOs.Moves;

namespace SC_Backend.Controllers
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
        [HttpGet]
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
        public async Task<IActionResult> PutMoveAsync(int id, string newMoveLog)
        {
            if(id <= 0 )
                return BadRequest("ID cannot be negative or 0");

            var move = await _movesRepository.GetAsync(id);
            if (move == null)
                return BadRequest($"Move with id {id} does not exist");

            move.MoveLog = newMoveLog;

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
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
            
        }

        // DELETE: api/Moves/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMoveAsyncAsync(int id)
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

        private static MoveDTO MapToDto(Move move)
        {
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
