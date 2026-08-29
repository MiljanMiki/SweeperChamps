using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;
using SC.Domain.DataModels;
using SC.Domain.DTOs;
using SC.Domain.Repositories.AsyncInterfaces;
using SC_Backend.Services;

namespace SC.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;

        public AuthController(IUserRepository repo, IAuthService authService)
        {
            _userRepository = repo;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponseDto>> Register(RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Provera da li username postoji
            try
            {
                if (await _userRepository.GetUserByUsernameAsync(registerDto.Username) != null)
                    return BadRequest(new { message = "Username već postoji" });

                // Provera da li email postoji
                if (await _userRepository.GetUserByEmailAsync(registerDto.Email) != null)
                    return BadRequest(new { message = "Email već postoji" });

                // Kreiranje korisnika
                var user = new User
                {
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    PasswordHash = _authService.HashPassword(registerDto.Password),
                    UserRole = UserRoles.User,
                    Datecreated = DateOnly.FromDateTime(DateTime.Now),
                };

                // Prvi korisnik postaje admin
                if (!await _userRepository.AnyUserExists())
                {
                    user.UserRole = UserRoles.Admin;
                }

                _userRepository.Add(user);
                await _userRepository.SaveChangesAsync();

                // Generisanje tokena
                var token = _authService.GenerateJwtToken(user);

                var response = new LoginResponseDto
                {
                    Token = token,
                    Expires = DateTime.UtcNow.AddHours(24),
                    User = new UserInfoDto
                    {
                        Id = user.UsersId,
                        Username = user.Username,
                        Email = user.Email,
                        Elo = user.Elo ?? 0,
                        CreatedAt = user.Datecreated
                    }
                };

                return Ok(response);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Pronalaženje korisnika po username
            var korisnik = await _userRepository.GetUserByUsernameAsync(loginDto.Username);

            if (korisnik == null)
                return NotFound($"User with username {loginDto.Username} does not exist.");

            if (!_authService.VerifyPassword(loginDto.Password, korisnik.PasswordHash))
                return Unauthorized(new { message = "Pogrešno korisničko ime ili lozinka" });

            // Generisanje tokena
            var token = _authService.GenerateJwtToken(korisnik);

            var response = new LoginResponseDto
            {
                Token = token,
                Expires = DateTime.UtcNow.AddHours(24),
                User = new UserInfoDto
                {
                    Id = korisnik.UsersId,
                    Username = korisnik.Username,
                    Email = korisnik.Email,
                    CreatedAt = korisnik.Datecreated,
                    Elo = korisnik.Elo ?? 0,
                }
            };

            return Ok(response);
        }
    }
}