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
    public class UserStatsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserStatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/UserStats
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserStats>>> GetUserStatsAsync()
        {
            return await _context.UserStats.ToListAsync();
        }

        // GET: api/UserStats/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserStats>> GetUserStatsAsync(int id)
        {
            var userStats = await _context.UserStats.FindAsync(id);

            if (userStats == null)
            {
                return NotFound();
            }

            return userStats;
        }

        // PUT: api/UserStats/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserStatsAsync(int id, UserStats userStats)
        {
            if (id != userStats.GameSettingId)
            {
                return BadRequest();
            }

            _context.Entry(userStats).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserStatsExists(id))
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

        // POST: api/UserStats
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<UserStats>> PostUserStatsAsync(UserStats userStats)
        {
            _context.UserStats.Add(userStats);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (UserStatsExists(userStats.GameSettingId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetUserStats", new { id = userStats.GameSettingId }, userStats);
        }

        // DELETE: api/UserStats/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserStatsAsync(int id)
        {
            var userStats = await _context.UserStats.FindAsync(id);
            if (userStats == null)
            {
                return NotFound();
            }

            _context.UserStats.Remove(userStats);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserStatsExists(int id)
        {
            return _context.UserStats.Any(e => e.GameSettingId == id);
        }
    }
}
