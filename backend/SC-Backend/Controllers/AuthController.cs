using Microsoft.AspNetCore.Mvc;
using SC_Backend.DataModels;
using SC_Backend.DTOs;
using SC_Backend.DataContext;
using SC_Backend.Services;

using Microsoft.EntityFrameworkCore;

namespace SC_Backend.Controllers;
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public AuthController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponseDto>> Register(RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Provera da li username postoji
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
                return BadRequest(new { message = "Username već postoji" });

            // Provera da li email postoji
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                return BadRequest(new { message = "Email već postoji" });

            // Kreiranje korisnika
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                //PasswordHash = _authService.HashPassword(registerDto.Password),
                UserRole = UserRoles.User,
                Datecreated = DateOnly.FromDateTime(DateTime.Now),
                //SlikaURL = registerDto.SlikaURL
            };

            // Prvi korisnik postaje admin
            if (!await _context.Users.AnyAsync())
            {
                user.UserRole = UserRoles.Admin;
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

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

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Pronalaženje korisnika po username ili email
            var korisnik = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username || u.Email == loginDto.Username);

            if (korisnik == null)
                return Unauthorized(new { message = "Pogrešno korisničko ime ili lozinka" });

            //if (!_authService.VerifyPassword(loginDto.Password, korisnik.PasswordHash))
              //  return Unauthorized(new { message = "Pogrešno korisničko ime ili lozinka" });

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
                    //SlikaUrl = korisnik.SlikaURL
                }
            };

            return Ok(response);
        }
    }
