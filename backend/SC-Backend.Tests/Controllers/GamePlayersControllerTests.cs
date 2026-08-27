using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NuGet.ContentModel;
using NUnit.Framework;
using SC_Backend.Controllers;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.GamePlayers;
using SC_Backend.DTOs.Games;
using SC_Backend.Repositories.AsyncImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Controllers
{
    [TestFixture]
    public class GamePlayersControllerTests
    {
        private ApplicationDbContext _context;
        private GamePlayerRepository _repository;
        private GamePlayersController _controller;

        [SetUp]
        public void Setup()
        {
            // Using standard standard libraries (EF Core In-Memory) to avoid external mocking dependencies
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new GamePlayerRepository(_context);
            _controller = new GamePlayersController(_repository);

            // Seed Initial Data
            _context.GamePlayers.Add(new GamePlayer
            {
                GamePlayersId = 1,
                GameId = 10,
                PlayerId = 5,
                TeamColor = TeamColors.Red,
                Score = 100
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
        public async Task GetGamePlayersAsync_ReturnsAllGamePlayers()
        {
            // Act
            var result = await _controller.GetGamePlayersAsync();

            // Assert
            Assert.That(result.Value, Is.Not.Null);
            var playersList = result.Value.ToList();
            Assert.That(playersList.Count, Is.EqualTo(1));
            Assert.That(playersList[0].Score, Is.EqualTo(100));
        }

        [Test]
        public async Task GetGamePlayerAsync_WithValidId_ReturnsGamePlayerDto()
        {
            // Act
            var result = await _controller.GetGamePlayerAsync(1);

            // Assert
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.GameId, Is.EqualTo(10));
            Assert.That(result.Value.Score, Is.EqualTo(100));
        }

        [Test]
        public async Task GetGamePlayerAsync_WithInvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetGamePlayerAsync(0);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PutGamePlayerAsync_WithValidData_UpdatesProperly()
        {
            // Arrange
            var putDto = new PutGamePlayerRequestDto
            {
                Score = 250,
                Outcome = Outcomes.Pending,
                Accuracy = 13.25,
                EloChange = 3,
                TeamColor = TeamColors.Blue

            };

            // Act
            var result = await _controller.PutGamePlayerAsync(1, putDto);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
            var updatedDbEntity = await _context.GamePlayers.FindAsync(1);
            Assert.That(updatedDbEntity.Score, Is.EqualTo(250));
            Assert.That(updatedDbEntity.TeamColor, Is.EqualTo(TeamColors.Blue));
        }

        [Test]
        public async Task PutGamePlayerAsync_WithNullDto_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.PutGamePlayerAsync(1, null);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task DeleteGamePlayerAsync_WithValidId_RemovesEntity()
        {
            // Act
            var result = await _controller.DeleteGamePlayerAsync(1);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
            var entityExists = await _context.GamePlayers.AnyAsync(gp => gp.GamePlayersId == 1);
            Assert.That(entityExists, Is.False);
        }

        [Test]
        public async Task GamesBetweenPlayersAsync_WithValidArray_ReturnsGames()
        {
            // Arrange
            int[] playerIds = { 5, 12 };
            // Simulate missing data gracefully handled by repository

            // Act
            var result = await _controller.GamesBetweenPlayersAsync(playerIds);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
        }

        #region GetLoadedGamePlayerAsync Tests

        [TestCase(0)]
        [TestCase(-1)]
        public async Task GetLoadedGamePlayerAsync_InvalidId_ReturnsBadRequest(int invalidId)
        {
            var result = await _controller.GetLoadedGamePlayerAsync(invalidId);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetLoadedGamePlayerAsync_WhenNotFound_ReturnsNotFound()
        {

            var result = await _controller.GetLoadedGamePlayerAsync(99);

            var notFound = result.Result as NotFoundObjectResult;
            Assert.That(notFound, Is.Not.Null);

            var errorMsg = notFound.Value as string;
            Assert.That(errorMsg,Does.Contain("does not exist"));
        }

        [Test]
        public async Task GetLoadedGamePlayerAsync_ValidId_ReturnsOkWithLoadedPlayerDto()
        {
            _context.Users.Add(new User
            {
                UsersId = 5,
                Username = "TestUser",
                Email = "test@test.com",
                PasswordHash = new string('a',60)
            });

            _context.Games.Add(new Game
            {
                GamesId = 10
            });

            // 2. Add your GamePlayer
            _context.GamePlayers.Add(new GamePlayer
            {
                GamePlayersId = 10,
                GameId = 10,
                PlayerId = 5,
                TeamColor = TeamColors.Red,
                Score = 100
            });

            await _context.SaveChangesAsync();

            var result = await _controller.GetLoadedGamePlayerAsync(10);

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var dto = okResult!.Value as LoadedPlayerDto;
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.User.Username, Is.EqualTo("TestUser"));
            Assert.That(dto.Game.GamesId, Is.EqualTo(10));
        }

        #endregion

        #region GetGamesFromSetting Tests

        [TestCase(0, 5)]
        [TestCase(1, -1)]
        public async Task GetGamesFromSetting_InvalidIds_ReturnsBadRequest(int playerId, int settingId)
        {
            var result = await _controller.GetGamesFromSetting(playerId, settingId);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetGamesFromSetting_PlayerNotFound_ReturnsNotFound()
        {

            var result = await _controller.GetGamesFromSetting(99, 1);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetGamesFromSetting_ValidRequest_ReturnsOkWithDtos()
        {
            int gameSettingId = 10;
            var setting = new GameSetting { GameSettingsId = gameSettingId };
            _context.GameSettings.Add(setting);

            
            _context.Games.Add(new Game { GamesId = 1, GameSettingsId = gameSettingId, GameSettings = setting });
            _context.GamePlayers.Add(new GamePlayer { GamePlayersId = 20, PlayerId = 1, GameId = 1 });

            await _context.SaveChangesAsync();

            var result = await _controller.GetGamesFromSetting(1, gameSettingId);

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var dtos = okResult!.Value as IEnumerable<GameDto>;
            Assert.That(dtos, Is.Not.Null);
            Assert.That(dtos!.Count(), Is.EqualTo(1));
        }

        #endregion

        #region GetUserMatchHistoryAsync Tests

        [Test]
        public async Task GetUserMatchHistoryAsync_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.GetUserMatchHistoryAsync(null!);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [TestCase(0, 1, 10)]
        [TestCase(1, -1, 10)]
        [TestCase(1, 1, 0)]
        public async Task GetUserMatchHistoryAsync_InvalidDtoProperties_ReturnsBadRequest(int pId, int page, int pageSize)
        {
            var dto = new MatchHistoryRequestDto { playerID= pId, page= page, pageSize = pageSize };

            var result = await _controller.GetUserMatchHistoryAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetUserMatchHistoryAsync_ValidRequest_ReturnsOkWithMatchHistoryDtos()
        {

            var dto = new MatchHistoryRequestDto { playerID = 5, page = 1, pageSize = 5 };
            
            _context.Games.Add(new Game { GamesId = 100, EndTime = DateTime.MinValue });
            _context.GamePlayers.Add(new GamePlayer { GamePlayersId = 20, GameId = 100, PlayerId = 5 });
            await _context.SaveChangesAsync();

            var result = await _controller.GetUserMatchHistoryAsync(dto);

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var dtos = okResult!.Value as IEnumerable<MatchHistoryDto>;
            Assert.That(dtos, Is.Not.Null);
            Assert.That(dtos!.Count(), Is.EqualTo(1));
            Assert.That(dtos!.First().Game.GamesId, Is.EqualTo(100));
        }

        #endregion

        #region UpdatePlayerResultsAsync Tests

        [Test]
        public async Task UpdatePlayerResultsAsync_NullList_ReturnsBadRequest()
        {
            var result = await _controller.UpdatePlayerResultsAsync(null!);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [TestCase(0, 100, 50)]
        [TestCase(1, -10, 50)]
        [TestCase(1, 100, -5)]
        public async Task UpdatePlayerResultsAsync_InvalidDataInList_ReturnsBadRequest(int id, int score, int accuracy)
        {
            var stats = new List<PlayerStatsRequestDto>
            {
                new PlayerStatsRequestDto { GamePlayerId = id, Score = score, Accuracy = accuracy }
            };

            var result = await _controller.UpdatePlayerResultsAsync(stats);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task UpdatePlayerResultsAsync_ValidData_UpdatesExistingPlayersAndSkipsMissing()
        {
            var stats = new List<PlayerStatsRequestDto>
            {
                new PlayerStatsRequestDto { GamePlayerId = 1, Score = 100, Outcome = Outcomes.Win, EloChange = 15, Accuracy = 90 },
                new PlayerStatsRequestDto { GamePlayerId = 2, Score = 50, Outcome = Outcomes.Loss, EloChange = -10, Accuracy = 40 } // Missing in DB
            };

            var gp1 = new GamePlayer { GamePlayersId = 1 };

            var result = await _controller.UpdatePlayerResultsAsync(stats);

            Assert.That(result, Is.InstanceOf<NoContentResult>());

        }

        #endregion

        #region GetTotalScoreForUserAsync Tests

        [TestCase(0)]
        [TestCase(-1)]
        public async Task GetTotalScoreForUserAsync_InvalidId_ReturnsBadRequest(int invalidId)
        {
            var result = await _controller.GetTotalScoreForUserAsync(invalidId);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetTotalScoreForUserAsync_ValidId_ReturnsOkWithScore()
        {

            var result = await _controller.GetTotalScoreForUserAsync(5);

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.Value, Is.EqualTo(100));
        }

        #endregion
    }
}
