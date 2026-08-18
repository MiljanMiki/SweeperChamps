using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SC_Backend.Controllers;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.GamePlayers;
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
    }
}