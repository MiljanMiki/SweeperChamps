using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.GameSettings;

namespace SC_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameSettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private const int minHeight = 10, maxHeight = 50, minWidth = 10, maxWidth = 50;

        public GameSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        //PUT i DELETE operacije nema
        #region CRUD
        // GET: api/GameSettings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GameSettingDto>>> GetGameSettingsAsync()
        {
            var list = await _context.GameSettings.ToListAsync();

            return list.Select(MapToDto).ToList();
        }

        // GET: api/GameSettings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GameSettingDto>> GetGameSettingAsync(int id)
        {
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0.");

            var gs = await _context.GameSettings.FindAsync(id);

            if (gs == null)
            {
                return NotFound();
            }

            return MapToDto(gs);
        }

        // POST: api/GameSettings
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<GameSettingDto>> PostGameSettingAsync(GameSettingDto dto)
        {
            if (dto == null)
                return BadRequest("Dto is null");

            var returnMessage = CheckDto(dto);
            if (returnMessage != null)
                return BadRequest(returnMessage);

            var gameSetting = new GameSetting
            {
                Width = dto.Width,
                Height = dto.Height,
                NumberOfMines = dto.NumberOfMines,
                StartTimeSeconds = dto.StartTimeSeconds,
                TeamSize = dto.TeamSize,
                WinCondition = dto.WinCondition,
                HasPowerUps = dto.HasPowerUps
            };
            _context.GameSettings.Add(gameSetting);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGameSettingAsync), new { id = gameSetting.GameSettingsId }, MapToDto(gameSetting));
        }
        #endregion CRUD

        [HttpPost("find-or-create")]
        public async Task<ActionResult<GameSettingDto>> GetOrCreateSettingAsync(GameSettingDto dto)
        {
            if(dto == null)
                return BadRequest("Dto is null");

            var returnMessage = CheckDto(dto);
            if (returnMessage != null)
                return BadRequest(returnMessage);

            // 1. Search for an exact match in the database
            var existingSetting = await _context.GameSettings.FirstOrDefaultAsync(gs =>
                gs.Width == dto.Width &&
                gs.Height == dto.Height &&
                gs.NumberOfMines == dto.NumberOfMines &&
                gs.StartTimeSeconds == dto.StartTimeSeconds &&
                gs.TeamSize == dto.TeamSize &&
                gs.WinCondition == dto.WinCondition &&
                gs.HasPowerUps == dto.HasPowerUps);

            if (existingSetting != null)
            {
                return Ok(MapToDto(existingSetting));
            }

            var newSetting = new GameSetting
            {
                Width = dto.Width,
                Height = dto.Height,
                NumberOfMines = dto.NumberOfMines,
                StartTimeSeconds = dto.StartTimeSeconds,
                TeamSize = dto.TeamSize,
                WinCondition = dto.WinCondition,
                HasPowerUps = dto.HasPowerUps
            };

            _context.GameSettings.Add(newSetting);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGameSettingAsync), new { id = newSetting.GameSettingsId }, MapToDto(newSetting));
        }

        [HttpGet("standard-modes")]
        public async Task<ActionResult<IEnumerable<GameSettingDto>>> GetStandardModesAsync()
        {
            // Example: Fetch known classic configurations without powerups
            var standardModes = await _context.GameSettings
                .Where(gs => !gs.HasPowerUps && gs.TeamSize == 1)
                .OrderBy(gs => gs.Width * gs.Height) // Order by board size
                .Take(3) // Beginner, Intermediate, Expert
                .ToListAsync();

            return Ok(standardModes.Select(MapToDto));
        }

        private static string? CheckDto(GameSettingDto dto)
        {
            if (dto.Height < minHeight || dto.Height > maxHeight)
                return $"Height must be in range between {minHeight},{maxHeight}.";
            if (dto.Width < minWidth || dto.Width > maxWidth)
                return $"Width must be in range between {minWidth},{maxWidth}.";           
            if (dto.NumberOfMines >= dto.Width * dto.Height)
                return "Board must have at least 1 cell without a mine.";
            if (dto.NumberOfMines <= 0)
                return "Board must have at least 1 mine";
            if (dto.StartTimeSeconds == null && dto.WinCondition != WinConditions.Race)
                return "No set time is allowed only for race mode.";
            if (dto.StartTimeSeconds != null &&
                dto.StartTimeSeconds >=30 &&
                dto.StartTimeSeconds <= 12000 &&
                dto.WinCondition == WinConditions.Race)
                return "Race mode is not allowed to have a set time.";
            if (dto.TeamSize <= 0)
                return "Team size cannot be negative or 0.";
            if (!Enum.IsDefined(typeof(WinConditions), dto.WinCondition))
                return "Enum value is not defined";

            return null;
            
        }

        private static GameSettingDto MapToDto(GameSetting gs)
        {
            return new GameSettingDto
            {
                GameSettingsId = gs.GameSettingsId,
                Width = gs.Width,
                Height = gs.Height,
                NumberOfMines = gs.NumberOfMines,
                StartTimeSeconds = gs.StartTimeSeconds,
                TeamSize = gs.TeamSize,
                WinCondition = gs.WinCondition,
                HasPowerUps = gs.HasPowerUps
            };
        }
        private bool GameSettingExists(int id)
        {
            return _context.GameSettings.Any(e => e.GameSettingsId == id);
        }
    }
}
