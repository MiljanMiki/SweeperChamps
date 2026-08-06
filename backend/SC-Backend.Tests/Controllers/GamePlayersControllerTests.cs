using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SC_Backend.Controllers;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.GamePlayers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Controllers
{
    [TestFixture]
    public class GamePlayersControllerTests
    {
        private ApplicationDbContext _context;
        private GamePlayersController _controller;

        [SetUp]
        public void Setup()
        {
            // Set up the In-Memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString()) // Unique DB for each test
                .Options;

            _context = new ApplicationDbContext(options);

            // Seed initial data
            _context.GamePlayers.Add(new GamePlayer
            {
                GamePlayersId = 1,
                GameId = 10,
                PlayerId = 5,
                TeamColor = TeamColors.Red,
                Score = 100
            });
            _context.SaveChanges();

            _controller = new GamePlayersController(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
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
        public async Task GetGamePlayerAsync_WithNonExistentId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetGamePlayerAsync(99);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task PutGamePlayerAsync_WithValidData_UpdatesSuccessfully()
        {
            // Arrange
            var updateDto = new PutGamePlayerRequestDto
            {
                Score = 150,
                TeamColor = TeamColors.Blue
            };

            // Act
            var result = await _controller.PutGamePlayerAsync(1, updateDto);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());

            var updatedEntity = await _context.GamePlayers.FindAsync(1);
            Assert.That(updatedEntity.Score, Is.EqualTo(150));
            Assert.That(updatedEntity.TeamColor, Is.EqualTo(TeamColors.Blue));
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
    }
}