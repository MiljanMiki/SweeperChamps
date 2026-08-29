using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC.Domain.DataModels;
using SC.Domain.DTOs.Users;
using SC.Domain.Repositories.AsyncInterfaces;

namespace SC.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository repo)
        {
            _userRepository = repo;
        }

        // GET: api/Users
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsersAsync()
        {
            var list = await _userRepository.GetAllAsync();
            return Ok(list.Select(MapToDto).ToList());
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDTO>> GetUserAsync(int id)
        {
            if(id<=0)
                return BadRequest("ID cannot be negative or 0.");

            var user = await _userRepository.GetAsync(id);

            if (user == null)
            {
                return NotFound($"{nameof(User)} with the ID {id} was not found.");
            }

            return Ok(MapToDto(user));
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserAsync(int id, UserUpdateDTO dto)
        {
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0.");
            if(dto == null)
                return BadRequest("DTO is null.");
            if (String.IsNullOrWhiteSpace(dto.Username) ||
                String.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("DTO string properties are null/empty/whitespace");
            if (!Enum.IsDefined(typeof(UserRoles), dto.UserRole))
                return BadRequest($"Enum {nameof(UserRoles)} does not have a defined value of {dto.UserRole}.");


            var user = await _userRepository.GetAsync(id);
            if(user == null)
                return NotFound($"Given {nameof(User)} ID does not exist in the database.");

            //ako bi postojale sezone, pa bi se na kraju svake sezone resetovao elo, ovo bi bilo lose...
            if (user.Elo != null && dto.Elo == null)
                return BadRequest($"User already has a set elo. It can only be reset to 0 now.");

            
            
            try
            {
                if (user.Username != dto.Username || user.Email != dto.Email)
                    if (!(await _userRepository.IsUniqueUsernameOrEmailAsync(dto.Username, dto.Email)))
                        return BadRequest($"Username or email is already taken.");

                user.Username = dto.Username;
                user.Email = dto.Email;
                user.UserRole = dto.UserRole;
                user.Elo = dto.Elo;
                await _userRepository.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await UserExists(id) == false)
                {
                    return NotFound($"{nameof(User)} with the ID {id} does not exist");
                }
                else
                {
                    throw;
                }
            }
            catch(ArgumentNullException e)
            {
                return BadRequest(e.Message);
            }

            return NoContent();
        }

        //Dodavanje bi trebalo iskljucivo preko AuthControllera da se radi
        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        
        //[HttpPost]
        //public async Task<ActionResult<User>> PostUserAsync(UserCreateDTO dto)
        //{
        //    if (dto == null)
        //        return BadRequest("DTO is null");
        //    if(!await _userRepository.IsUniqueUsernameOrEmailAsync(dto.Username,dto.Email))
        //        return BadRequest($"Username or email is already taken.");


        //    try
        //    {
        //        var user = new User {
        //            Username=dto.Username,
        //            Email = dto.Email,
        //            Datecreated= DateOnly.FromDateTime(DateTime.Now),
        //            Elo = null,
                    
        //        };

        //        _userRepository.Add(user);
        //        await _userRepository.SaveChangesAsync();

        //        return CreatedAtAction("GetUser", new { id = user.UsersId }, user);
        //    }
        //    catch(Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserAsync(int id)
        {
            if(id<=0)
                return BadRequest("ID cannot be negative or 0.");

            var user = await _userRepository.GetAsync(id);
            if (user == null)
            {
                return NotFound($"{nameof(User)} with ID {id} was not found.");
            }

            _userRepository.Delete(user);
            await _userRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("leaderboard/{topCount}")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetLeaderboard(int topCount)
        {
            if (topCount <= 0)
                return BadRequest("Leaderboard size must be larger than 0.");

            return Ok(await _userRepository.GetLeaderboardAsync(topCount));
        }

        [HttpGet("loaded-user/{id}/{history}/{stats}")]
        public async Task<ActionResult<LoadedUserDTO>> GetLoadedUser(int id, bool history, bool stats)
        {
            if(id<=0)
                return BadRequest("ID cannot be negative or 0.");
            var user = await _userRepository.GetUserWithLoadedPropertiesAsync(id, history, stats);

            if (user == null)
                return NotFound($"{nameof(User)} with ID {id} was not found.");

            List<GamesHistoryDTO>? historyDTO = null;
            if(history)
            {
                historyDTO = user.GamePlayers.Select(gp => new GamesHistoryDTO
                {
                    GameId = gp.GameId,
                    PlayerId = gp.PlayerId,
                    TeamColor = gp.TeamColor,
                    Score = gp.Score
                }).ToList();
            }

            List<UserStatsDTO>? statsDTO = null;
            if(stats)
            {
                statsDTO = user.UserStats.Select(s => new UserStatsDTO
                {
                    GameSettingId = s.GameSettingId,
                    IsRanked = s.IsRanked,
                    GamesPlayed = s.GamesPlayed,
                    Wins = s.Wins,
                    Losses = s.Losses,
                    PlayTime = s.PlayTime
                }).ToList();
            }

            var dto = new LoadedUserDTO
            {
                UserData = new UserDTO
                {
                    Username = user.Username,
                    Email = user.Email,
                    Datecreated = user.Datecreated,
                    Elo = user.Elo,
                    UserRole = user.UserRole
                },
                GameHistory = historyDTO,
                Stats = statsDTO
            };

            return dto;
        }

        [HttpGet("filter-users")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> FilterUsers([FromQuery] UserFilteringDTO dto)
        {
            if (dto == null)
                return BadRequest("DTO is null");
            if (dto.Elo == null && dto.DateCreated == null && dto.EloBigger == null && dto.DateBefore == null)
                return BadRequest("All DTO properties are null. Atleast 1 must have a non null value");
            if (dto.Elo.HasValue && dto.Elo <= 0)
                return BadRequest("Elo must be positive");
            if(dto.Role.HasValue && !Enum.IsDefined(typeof(UserRoles),dto.Role))
                return BadRequest($"{dto.Role} value is not defined for enum ${nameof(UserRoles)}");

            try
            {
                var list = await _userRepository.FilterUsersAsync(dto.DateCreated, dto.DateBefore, dto.Elo, dto.EloBigger, dto.Role);

                return Ok(list.Select(MapToDto));
            }
            catch(ArgumentNullException e)
            {
                return BadRequest(e.Message);
            }
        }

        private static UserDTO MapToDto(User user)
        {
            return new UserDTO
            {
                Username = user.Username,
                Email = user.Email,
                Datecreated = user.Datecreated,
                Elo = user.Elo,
                UserRole = user.UserRole
            };
        }

        private async Task<bool> UserExists(int id)
        {
            return await _userRepository.GetAsync(id) != null;
        }
    }
}
