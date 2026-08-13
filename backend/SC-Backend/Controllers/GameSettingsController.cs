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
using SC_Backend.Repositories;

namespace SC_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameSettingsController : ControllerBase
    {
        private readonly IGameSettingRepository _gameSettingRepository;
        private const int minHeight = 10, maxHeight = 50, minWidth = 10, maxWidth = 50;

        public GameSettingsController(IGameSettingRepository repo)
        {
            _gameSettingRepository = repo;
        }

        //PUT i DELETE operacije nema
        #region CRUD
        // GET: api/GameSettings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GameSettingDto>>> GetGameSettingsAsync()
        {
            var list = await _gameSettingRepository.GetAllAsync();

            return Ok(list.Select(MapToDto).ToList());
        }

        // GET: api/GameSettings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GameSettingDto>> GetGameSettingAsync(int id)
        {
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0.");

            var gs = await _gameSettingRepository.GetAsync(id);

            if (gs == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(gs));
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
            try
            {
                _gameSettingRepository.Add(gameSetting);
                await _gameSettingRepository.SaveChangesAsync();

                return CreatedAtAction(nameof(GetGameSettingAsync), new { id = gameSetting.GameSettingsId }, MapToDto(gameSetting));
            }
            catch(ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
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

            GameSetting gs = new GameSetting
            {
                Width = dto.Width,
                Height = dto.Height,
                NumberOfMines = dto.NumberOfMines,
                StartTimeSeconds = dto.StartTimeSeconds,
                TeamSize = dto.TeamSize,
                WinCondition = dto.WinCondition,
                HasPowerUps = dto.HasPowerUps
            };
            
            
            var setting = await _gameSettingRepository.GetOrCreateSettingAsync(gs);

            if (setting != null)
            {
                return Ok(MapToDto(setting));
            }
            else
            {
                return CreatedAtAction(nameof(GetGameSettingAsync), new { id = gs.GameSettingsId }, MapToDto(gs));
            }
        }

        [HttpGet("standard-modes")]
        public async Task<ActionResult<IEnumerable<GameSettingDto>>> GetStandardModesAsync()
        {
            var modes = await _gameSettingRepository.GetStandardModesAsync();

            return Ok(modes.Select(MapToDto));
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

            if (dto.WinCondition == WinConditions.Race && dto.StartTimeSeconds != null)
                return "Race mode is not allowed to have a set time.";
            if (dto.WinCondition != WinConditions.Race)
            {
                if (dto.StartTimeSeconds == null)
                    return "No set time is allowed only for race mode.";
                if (dto.StartTimeSeconds < 30 || dto.StartTimeSeconds > 12000)
                    return "Start time must be between 30 and 12000 seconds.";
            }

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
                Width = gs.Width,
                Height = gs.Height,
                NumberOfMines = gs.NumberOfMines,
                StartTimeSeconds = gs.StartTimeSeconds,
                TeamSize = gs.TeamSize,
                WinCondition = gs.WinCondition,
                HasPowerUps = gs.HasPowerUps
            };
        }
        private async Task<bool> GameSettingExists(int id)
        {
            return await _gameSettingRepository.GetAsync(id) != null;
        }
    }
}
