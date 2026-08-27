using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using SC_Backend.Controllers;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.Games;
using SC_Backend.Repositories.AsyncImplementations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Controllers
{
    [TestFixture]
    public class GamesControllerTests
    {
        private ApplicationDbContext _context;
        private GameRepository _repository;
        private GamesController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new GameRepository(_context);
            _controller = new GamesController(_repository);

            _context.GameSettings.Add(new GameSetting { GameSettingsId = 1 });
            _context.Games.Add(new Game
            {
                GamesId = 1,
                StartTime = new DateTime(2026, 8, 1, 10, 0, 0),
                EndTime = new DateTime(2026, 8, 1, 10, 30, 0),
                Status = GameStatuses.Finished,
                GameSettingsId = 1
            });
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetGamesAsync_ReturnsGamesList()
        {
            var result = await _controller.GetGamesAsync();

            Assert.That(result.Value, Is.Not.Null);
            var list = result.Value.ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0].GamesId, Is.EqualTo(1));
        }

        [Test]
        public async Task GetGameAsync_WithValidId_ReturnsGameDto()
        {
            var result = await _controller.GetGameAsync(1);

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var dto = okResult.Value as GameDto;
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.GamesId, Is.EqualTo(1));
            
        }

        [Test]
        public async Task GetGameAsync_WithInvalidId_ReturnsBadRequest()
        {
            var result = await _controller.GetGameAsync(0);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PutGameAsync_WithValidData_UpdatesSuccessfully()
        {
            var putDto = new PutGameDto
            {
                EndTime = new DateTime(2026, 8, 1, 11, 0, 0),
                Status = GameStatuses.Finished
            };

            var result = await _controller.PutGameAsync(1, putDto);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            var updatedEntity = await _context.Games.FindAsync(1);
            Assert.That(updatedEntity.EndTime, Is.EqualTo(new DateTime(2026, 8, 1, 11, 0, 0)));
        }

        [Test]
        public async Task PutGameAsync_WithNullDto_ReturnsBadRequest()
        {
            var result = await _controller.PutGameAsync(1, null);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PostGameAsync_WithValidData_CreatesGame()
        {
            var postDto = new PostGameDto
            {
                StartTime = new DateTime(2026, 8, 2, 10, 0, 0),
                Status = GameStatuses.InProgress,
                GameSettingsId = 1
            };

            var result = await _controller.PostGameAsync(postDto);

            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        }

        [Test]
        public async Task PostGameAsync_WithInvalidGameSettingId_ReturnsBadRequest()
        {
            var postDto = new PostGameDto
            {
                StartTime = new DateTime(2026, 8, 2, 10, 0, 0),
                Status = GameStatuses.InProgress,
                GameSettingsId = 99
            };

            var result = await _controller.PostGameAsync(postDto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task DeleteGameAsync_WithValidId_DeletesGame()
        {
            var result = await _controller.DeleteGameAsync(1);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            var entityExists = await _context.Games.AnyAsync(g => g.GamesId == 1);
            Assert.That(entityExists, Is.False);
        }

        [Test]
        public async Task FilterGameByStatusAndDateAsync_WithInvalidDateParams_ReturnsBadRequest()
        {
            var result = await _controller.FilterGameByStatusAndDateAsync(GameStatuses.Finished, new DateTime(2026, 8, 1));

            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Is.EqualTo("Day, month or year must be specified for date filtering."));
        }

        #region GetLiveGamesAsync Tests

        [TestCase(0)]
        [TestCase(-1)]
        public async Task GetLiveGamesAsync_InvalidLimit_ReturnsBadRequest(int invalidLimit)
        {
            var result = await _controller.GetLiveGamesAsync(invalidLimit);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetLiveGamesAsync_ValidLimit_ReturnsMappedDtos()
        {
            var liveGames = new List<Game>
            {
                new Game { GamesId = 10, StartTime = DateTime.UtcNow.AddMinutes(-5), EndTime = null },
                new Game { GamesId = 20, StartTime = DateTime.UtcNow.AddMinutes(-2), EndTime = null }
            };

            foreach (var g in liveGames)
                _context.Games.Add(g);
            await _context.SaveChangesAsync();

            var result = await _controller.GetLiveGamesAsync(10);

            // Since returning list directly in ActionResult<T>, ASP.NET Core wraps or exposes value
            var dtos = result.Value as IEnumerable<GameDto> ?? (result.Result as OkObjectResult)?.Value as IEnumerable<GameDto>;

            Assert.That(dtos, Is.Not.Null);
            Assert.That(dtos.Count(), Is.EqualTo(2));
        }

        #endregion

        #region MarkGameFinishedAsync Tests

        [TestCase(0, 300, TeamColors.Red, "ID cannot be negative or 0")]
        [TestCase(-1, 300, TeamColors.Red, "ID cannot be negative or 0")]
        [TestCase(1, 0, TeamColors.Red, "Duration cannot be negative or 0")]
        [TestCase(1, -10, TeamColors.Red, "Duration cannot be negative or 0")]
        public async Task MarkGameFinishedAsync_InvalidParameters_ReturnsBadRequest(
            int gameId, int durationSeconds, TeamColors winningTeam, string expectedError)
        {
            var result = await _controller.MarkGameFinishedAsync(gameId, durationSeconds, winningTeam);

            var badRequest = result as BadRequestObjectResult;
            Assert.That(badRequest, Is.Not.Null);
            Assert.That(badRequest!.Value, Is.EqualTo(expectedError));
        }

        [Test]
        public async Task MarkGameFinishedAsync_UndefinedEnumValue_ReturnsBadRequest()
        {
            var invalidTeam = (TeamColors)99;

            var result = await _controller.MarkGameFinishedAsync(gameId: 1, durationSeconds: 120, winningTeam: invalidTeam);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        [Ignore("Cannot test ExecuteUpdateAsync.")]
        public async Task MarkGameFinishedAsync_ValidInputs_ReturnsNoContentAndCallsRepository()
        {
            int gameId = 1;
            int duration = 600;
            var winningTeam = TeamColors.Red;

            var result = await _controller.MarkGameFinishedAsync(gameId, duration, winningTeam);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        #endregion

        #region GetPlayersGamesAsync Tests

        [TestCase(0, 10, true)]
        [TestCase(-1, 10, false)]
        [TestCase(1, 0, true)]
        [TestCase(1, -5, false)]
        public async Task GetPlayersGamesAsync_InvalidIdOrLimit_ReturnsBadRequest(int playerId, int limit, bool isRanked)
        {
            var result = await _controller.GetPlayersGamesAsync(playerId, limit, isRanked);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetPlayersGamesAsync_PlayerNotFound_ReturnsNotFound()
        {

            var result = await _controller.GetPlayersGamesAsync(playerID: 99, limit: 10, isRanked: true);

            var notFoundResult = result.Result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult!.Value, Is.EqualTo("User with ID 99 does not exist."));
        }

        [Test]
        public async Task GetPlayersGamesAsync_ValidRequest_ReturnsOkWithDtos()
        {
            // 1. Seed the User
            _context.Users.Add(new User
            {
                UsersId = 1,
                Username = "username",
                Email = "nesto@gmail.com",
                PasswordHash = new string('a', 60)
            });

            // 2. Seed Games with distinct GamePlayer instances
            var games = new List<Game>
            {
                new Game { GamesId = 10, IsRanked = true, GamePlayers = new List<GamePlayer> { new GamePlayer { PlayerId = 1 } } },
                new Game { GamesId = 11, IsRanked = true, GamePlayers = new List<GamePlayer> { new GamePlayer { PlayerId = 1 } } },
                new Game { GamesId = 12, IsRanked = true, GamePlayers = new List<GamePlayer> { new GamePlayer { PlayerId = 1 } } },
                new Game { GamesId = 13, IsRanked = true, GamePlayers = new List<GamePlayer> { new GamePlayer { PlayerId = 1 } } }
            };

            _context.Games.AddRange(games);
            await _context.SaveChangesAsync();

            // 3. Act
            var result = await _controller.GetPlayersGamesAsync(playerID: 1, limit: 5, isRanked: true);

            // 4. Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var dtos = okResult!.Value as IEnumerable<GameDto>;
            Assert.That(dtos, Is.Not.Null);
            Assert.That(dtos!.Count(), Is.EqualTo(games.Count));
        }

        #endregion

    }
}