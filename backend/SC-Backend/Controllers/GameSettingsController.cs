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
    public class GameSettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GameSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/GameSettings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GameSetting>>> GetGameSettingsAsync()
        {
            return await _context.GameSettings.ToListAsync();
        }

        // GET: api/GameSettings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GameSetting>> GetGameSettingAsync(int id)
        {
            var gameSetting = await _context.GameSettings.FindAsync(id);

            if (gameSetting == null)
            {
                return NotFound();
            }

            return gameSetting;
        }

        // PUT: api/GameSettings/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGameSettingAsync(int id, GameSetting gameSetting)
        {
            if (id != gameSetting.GameSettingsId)
            {
                return BadRequest();
            }

            _context.Entry(gameSetting).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GameSettingExists(id))
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

        // POST: api/GameSettings
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<GameSetting>> PostGameSettingAsync(GameSetting gameSetting)
        {
            _context.GameSettings.Add(gameSetting);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGameSetting", new { id = gameSetting.GameSettingsId }, gameSetting);
        }

        // DELETE: api/GameSettings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGameSettingAsync(int id)
        {
            var gameSetting = await _context.GameSettings.FindAsync(id);
            if (gameSetting == null)
            {
                return NotFound();
            }

            _context.GameSettings.Remove(gameSetting);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GameSettingExists(int id)
        {
            return _context.GameSettings.Any(e => e.GameSettingsId == id);
        }
    }
}
