using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SC_Backend.Controllers;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Controllers
{
    [TestFixture]
    public class GamesControllerTests
    {
        private ApplicationDbContext _context;
        private GamesController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            // Seed initial GameSettings to satisfy the Foreign Key constraint
            _context.GameSettings.Add(new GameSetting { GameSettingsId = 1 });

            // Seed initial Game
            _context.Games.Add(new Game
            {
                GamesId = 1,
                StartTime = new DateTime(2026, 8, 1, 10, 0, 0),
                EndTime = new DateTime(2026, 8, 1, 10, 30, 0),
                Status = GameStatuses.Finished,
                GameSettingsId = 1
            });
            _context.SaveChanges();

            _controller = new GamesController(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetGameAsync_WithValidId_ReturnsGameDto()
        {
            // Act
            var result = await _controller.GetGameAsync(1);

            // Assert
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.GamesId, Is.EqualTo(1));
            Assert.That(result.Value.Status, Is.EqualTo(GameStatuses.Finished));
        }

        [Test]
        public async Task PutGameAsync_WithValidData_UpdatesSuccessfully()
        {
            // Arrange
            var putDto = new PutGameDto
            {
                EndTime = new DateTime(2026, 8, 1, 11, 0, 0),
                Status = GameStatuses.Terminated
            };

            // Act
            var result = await _controller.PutGameAsync(1, putDto);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());

            var updatedEntity = await _context.Games.FindAsync(1);
            Assert.That(updatedEntity.Status, Is.EqualTo(GameStatuses.Terminated));
            Assert.That(updatedEntity.EndTime, Is.EqualTo(new DateTime(2026, 8, 1, 11, 0, 0)));
        }

        [Test]
        public async Task PutGameAsync_WithInvalidDate_ReturnsBadRequest()
        {
            // Arrange: EndTime is before StartTime
            var putDto = new PutGameDto
            {
                EndTime = new DateTime(2026, 8, 1, 9, 0, 0),
                Status = GameStatuses.Finished
            };

            // Act
            var result = await _controller.PutGameAsync(1, putDto);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult.Value, Is.EqualTo("Invalid date: a game cannot end before it started."));
        }

        [Test]
        public async Task PostGameAsync_WithValidData_CreatesGame()
        {
            // Arrange
            var postDto = new PostGameDto
            {
                StartTime = new DateTime(2026, 8, 2, 10, 0, 0),
                EndTime = null,
                Status = GameStatuses.InProgress,
                GameSettingsId = 1
            };

            // Act
            var result = await _controller.PostGameAsync(postDto);

            // Assert
            var createdResult = result.Result as CreatedAtActionResult;
            Assert.That(createdResult, Is.Not.Null);

            var createdGame = createdResult.Value as Game;
            Assert.That(createdGame, Is.Not.Null);
            Assert.That(createdGame.Status, Is.EqualTo(GameStatuses.InProgress));
        }

        [Test]
        public async Task FilterGameByStatusAndDate_WithValidMonth_ReturnsFilteredGames()
        {
            // Act
            var filterDate = new DateTime(2026, 8, 1);
            var result = await _controller.FilterGameByStatusAndDate(GameStatuses.Finished, filterDate, month: true);

            // Assert
            Assert.That(result.Value, Is.Not.Null);
            var games = result.Value.ToList();
            Assert.That(games.Count, Is.EqualTo(1));
            Assert.That(games.First().GamesId, Is.EqualTo(1));
        }
    }
}