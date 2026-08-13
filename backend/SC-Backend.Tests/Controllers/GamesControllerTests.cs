using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SC_Backend.Controllers;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.Games;
using SC_Backend.Repositories;
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

            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.GamesId, Is.EqualTo(1));
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
    }
}