using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;

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

        // GET: api/GamePlayers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GamePlayer>>> GetGamePlayersAsync()
        {
            return await _context.GamePlayers.ToListAsync();
        }

        // GET: api/GamePlayers/5
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
        public async Task<IActionResult> PutGamePlayerAsync(int id, GamePlayer gamePlayer)
        {
            if (id != gamePlayer.GamePlayersId)
            {
                return BadRequest();
            }

            _context.Entry(gamePlayer).State = EntityState.Modified;

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
        public async Task<ActionResult<GamePlayer>> PostGamePlayerAsync(GamePlayer gamePlayer)
        {
            _context.GamePlayers.Add(gamePlayer);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGamePlayer", new { id = gamePlayer.GamePlayersId }, gamePlayer);
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

        private bool GamePlayerExists(int id)
        {
            return _context.GamePlayers.Any(e => e.GamePlayersId == id);
        }
    }
}
