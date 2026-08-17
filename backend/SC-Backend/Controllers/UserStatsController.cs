using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.GameSettings;
using SC_Backend.DTOs.Users;
using SC_Backend.DTOs.UserStats;
using SC_Backend.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserStatsController : ControllerBase
    {
        private readonly IUserStatsRepository _userStatsRepository;

        public UserStatsController(IUserStatsRepository userStatsRepository)
        {
            _userStatsRepository = userStatsRepository;
        }

        // GET: api/UserStats
        [HttpGet("get-all")]
        public async Task<ActionResult<IEnumerable<UserStatDTO>>> GetUserStatsAsync()
        {
            return Ok((await _userStatsRepository.GetAllAsync()).Select(MapToDto).ToList());
        }

        // GET: api/UserStats/5
        [HttpGet("{userID}/{gameSettingID}/{isRanked}")]
        public async Task<ActionResult<UserStatDTO>> GetUserStatsAsync(int userID,int gameSettingID, bool isRanked)
        {
            if (userID <= 0 || gameSettingID <= 0)
                return BadRequest("FK values cannot be less than or equal to 0");

            try
            {
                var userStats = await _userStatsRepository.GetStatAsync(userID, gameSettingID, isRanked);

                if (userStats == null)
                {
                    return NotFound($"{nameof(UserStats)} does not exist with the given FK ids");
                }

                return Ok(MapToDto(userStats));
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // PUT: api/UserStats/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("put")]
        public async Task<IActionResult> PutUserStatsAsync(FullStatDTO dto)
        {
            var retMessage = CheckFullDTO(dto);
            if (retMessage != null)
                return BadRequest(retMessage);


            try
            {
                var stat = await _userStatsRepository.GetStatAsync(dto.UserId, dto.GameSettingId, dto.IsRanked);
                if(stat == null)
                    return NotFound($"{nameof(UserStats)} does not exist with the given FK ids");

                stat.GamesPlayed = dto.GamesPlayed;
                stat.Wins = dto.Wins;
                stat.Losses = dto.Losses;
                stat.PlayTime = dto.PlayTime;

                await _userStatsRepository.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await UserStatsExists(dto.UserId,dto.GameSettingId,dto.IsRanked))
                {
                    return NotFound("Stat with passed FKs does not exist.");
                }
                else
                {
                    throw;
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return NoContent();
        }

        // POST: api/UserStats
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<UserStats>> PostUserStatsAsync(FullStatDTO dto)
        {
            var retMessage = CheckFullDTO(dto);
            if (retMessage != null)
                return BadRequest(retMessage);

            var userStats = new UserStats
            {
                GameSettingId = dto.GameSettingId,
                UserId = dto.UserId,
                IsRanked = dto.IsRanked,
                GamesPlayed = dto.GamesPlayed,
                Wins = dto.Wins,
                Losses = dto.Losses,
                PlayTime = dto.PlayTime
            };

            try
            {

                _userStatsRepository.Add(userStats);
                await _userStatsRepository.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUserStatsAsync), new { userID = userStats.UserId, gameSettingID = userStats.GameSettingId, isRanked = userStats.IsRanked }, userStats);

            }
            catch (DbUpdateException)
            {
                if (await UserStatsExists(userStats.UserId, userStats.GameSettingId,userStats.IsRanked))
                {
                    return Conflict($"User stat already exists with these keys: UserID:{userStats.UserId}, GameSettign: {userStats.GameSettingId}, IsRanked: {userStats.IsRanked}");
                }
                else
                {
                    throw;
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

        }
        
        // DELETE: api/UserStats/5
        [HttpDelete("{userID}/{gameSettingID}/{isRanked}")]
        public async Task<IActionResult> DeleteUserStatsAsync(int userID,int gameSettingID,bool isRanked)
        {
            var userStats = await _userStatsRepository.GetStatAsync(userID, gameSettingID, isRanked);
            if (userStats == null)
            {
                return NotFound($"User stat doesnt exist with these keys: UserID:{userID}, GameSettign: {gameSettingID}, IsRanked: {isRanked}");
            }

            try
            {
                _userStatsRepository.Delete(userStats);
                await _userStatsRepository.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("loaded")]
        public async Task<ActionResult<LoadedStatDto?>> GetLoadedStatAsync(int userID, int gameSettingID, bool isRanked)
        {
            try
            {
                var stat = await _userStatsRepository.GetStatsWithLoadedPropertiesAsync(userID, gameSettingID, isRanked);
                if (stat == null)
                    return NotFound($"User stat doesnt exist with these keys: UserID:{userID}, GameSettign: {gameSettingID}, IsRanked: {isRanked}");

                return Ok(MakeLoadedStat(stat, true, true));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Can be for a specific gamemode/setting 
        [HttpGet("user-allstats")]
        public async Task<ActionResult<IEnumerable<FullStatDTO>>> GetAllUserStatsAsync(int userID, int? gameSettingID, bool isRanked, bool loadNav)
        {
            if (userID <= 0 || gameSettingID <= 0)
                return BadRequest("FK ids cannot be negative or 0.");
            try
            {
                var stats = await _userStatsRepository.GetAllStatsOfUserAsync(userID, gameSettingID, isRanked, loadNav);

                return Ok(stats.Select(s => MakeLoadedStat(s, loadNav, gameSettingID.HasValue && loadNav)).ToList()); 
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("leaderboard")]
        public async Task<ActionResult<IEnumerable<LeaderboardDTO>>> GetLeaderboardAsync(int gameSettingId, bool isRanked, int topCount)
        {
            if (gameSettingId <= 0)
                return BadRequest($"FK to {nameof(GameSetting)} cannot be negative or 0.");

            try
            {
                var leaderboard = await _userStatsRepository.GetTopPlayersForSettingAsync(gameSettingId, isRanked, topCount);

                return Ok(leaderboard.Select(s => new LeaderboardDTO
                {
                    Username = s.User.Username,
                    Elo = s.User.Elo,
                    GameSettingId = s.GameSettingId,
                    UserStat = MapToDto(s),
                    WinRatePercentage = s.GamesPlayed == 0 ? 0 : ((double)s.Wins / s.GamesPlayed) * 100
                }).ToList());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("record-game-end")]
        public async Task<ActionResult>RecordGameEndingAsync(int userId, int gameSettingId, bool isRanked, bool isWin, long matchDuration)
        {
            if (userId<= 0)
                return BadRequest($"FK to {nameof(User)} cannot be negative or 0.");
            if (gameSettingId <= 0)
                return BadRequest($"FK to {nameof(GameSetting)} cannot be negative or 0.");
            if (matchDuration < 0)
                return BadRequest("Match duration cannot be negative.");

            try
            {
                await _userStatsRepository.RecordMatchResultAsync(userId, gameSettingId, isRanked, isWin, matchDuration);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        private static UserStatDTO MapToDto(UserStats stats)
        {
            return new UserStatDTO
            {
                GamesPlayed = stats.GamesPlayed,
                Wins = stats.Wins,
                Losses = stats.Losses,
                IsRanked = stats.IsRanked,
                PlayTime = stats.PlayTime,
            };
        }

        private static string? CheckFullDTO(FullStatDTO dto)
        {
            if (dto == null)
                return "DTO is null";

            if (dto.GameSettingId <= 0 || dto.UserId <= 0)
                return "FK to GameSetting/User cannot be negative or 0";

            if (dto.GamesPlayed < 0 || dto.Wins < 0 || dto.Losses < 0 || dto.PlayTime < 0)
                return "None of the following attributes can be negative: GamesPlayed, Wins, Losses and PlayTime";

            if ((dto.Wins + dto.Losses) > dto.GamesPlayed)
                return "Sum of Wins+Losses cannot be greater than GamesPlayed";

            return null;
        }

        private static LoadedStatDto MakeLoadedStat(UserStats stat,bool userLoaded, bool settingsLoaded)
        {
            UserDTO? user = null;
            if(userLoaded)
            {
                user = new UserDTO
                {
                    Username = stat.User.Username,
                    Email = stat.User.Email,
                    Datecreated = stat.User.Datecreated,
                    Elo = stat.User.Elo,
                    UserRole = stat.User.UserRole
                };
            }

            GameSettingDto? setting = null;
            if (settingsLoaded)
            {
                setting = new GameSettingDto
                {
                    Width = stat.GameSetting.Width,
                    Height = stat.GameSetting.Height,
                    NumberOfMines = stat.GameSetting.NumberOfMines,
                    StartTimeSeconds = stat.GameSetting.StartTimeSeconds,
                    TeamSize = stat.GameSetting.TeamSize,
                    WinCondition = stat.GameSetting.WinCondition,
                    HasPowerUps = stat.GameSetting.HasPowerUps
                };
            }
            return new LoadedStatDto
            {
                GamesPlayed = stat.GamesPlayed,
                Wins = stat.Wins,
                Losses = stat.Losses,
                PlayTime = stat.PlayTime,
                IsRanked = stat.IsRanked,
                SettingSummary = setting,
                UserSummary = user
            };
        }
        private async Task<bool> UserStatsExists(int playerID, int gameSettingID, bool isRanked)
        {
            return await _userStatsRepository.GetStatAsync(playerID,gameSettingID,isRanked) != null;
        }
    }
}
