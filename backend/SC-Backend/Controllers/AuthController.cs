using Microsoft.AspNetCore.Mvc;
using SC_Backend.Models;
using SC_Backend.DTOs;
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
            if (await _context.Korisnici.AnyAsync(u => u.Username == registerDto.Username))
                return BadRequest(new { message = "Username već postoji" });

            // Provera da li email postoji
            if (await _context.Korisnici.AnyAsync(u => u.Email == registerDto.Email))
                return BadRequest(new { message = "Email već postoji" });

            // Kreiranje korisnika
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = _authService.HashPassword(registerDto.Password),
                Role = UserRoles.User,
                CreatedAt = DateTime.UtcNow,
                SlikaURL = registerDto.SlikaURL
            };

            // Prvi korisnik postaje admin
            if (!await _context.Korisnici.AnyAsync())
            {
                user.Role = UserRoles.Admin;
            }

            _context.Korisnici.Add(user);
            await _context.SaveChangesAsync();

            // Generisanje tokena
            var token = _authService.GenerateJwtToken(user);

            var response = new LoginResponseDto
            {
                Token = token,
                Expires = DateTime.UtcNow.AddHours(24),
                User = new UserInfoDto
                {
                    Id = user.ID,
                    Username = user.Username,
                    Email = user.Email,
                    Elo = user.Elo,
                    CreatedAt = user.CreatedAt
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
            var korisnik = await _context.Korisnici
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username || u.Email == loginDto.Username);

            if (korisnik == null)
                return Unauthorized(new { message = "Pogrešno korisničko ime ili lozinka" });

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
                    Id = korisnik.ID,
                    Username = korisnik.Username,
                    Email = korisnik.Email,
                    CreatedAt = korisnik.CreatedAt,
                    Elo = korisnik.Elo,
                    SlikaUrl = korisnik.SlikaURL
                }
            };

            return Ok(response);
        }
    }
