using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SC.Api.Controllers;
using SC_Backend.DataContext;
using SC.Domain.DataModels;
using SC.Domain.DTOs.GameSettings;
using SC_Backend.Repositories.AsyncImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Controllers
{
    [TestFixture]
    public class GameSettingsControllerTests
    {
        private ApplicationDbContext _context;
        private GameSettingRepository _repository;
        private GameSettingsController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new GameSettingRepository(_context);
            _controller = new GameSettingsController(_repository);

            _context.GameSettings.Add(new GameSetting
            {
                GameSettingsId = 1,
                Width = 20,
                Height = 20,
                NumberOfMines = 50,
                StartTimeSeconds = 300,
                TeamSize = 1,
                WinCondition = WinConditions.TimeRush,
                HasPowerUps = false
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
        public async Task GetGameSettingsAsync_ReturnsAllSettings()
        {
            var result = await _controller.GetGameSettingsAsync();

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var list = okResult.Value as IEnumerable<GameSettingDto>;
            Assert.That(list, Is.Not.Null);
            Assert.That(list.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetGameSettingAsync_WithValidId_ReturnsSetting()
        {
            var result = await _controller.GetGameSettingAsync(1);

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var dto = okResult.Value as GameSettingDto;
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.Width, Is.EqualTo(20));
        }

        [Test]
        public async Task GetGameSettingAsync_WithInvalidId_ReturnsBadRequest()
        {
            var result = await _controller.GetGameSettingAsync(0);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetGameSettingAsync_WithNonExistentId_ReturnsNotFound()
        {
            var result = await _controller.GetGameSettingAsync(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task PostGameSettingAsync_WithValidDto_CreatesSetting()
        {
            var dto = new GameSettingDto
            {
                Width = 15,
                Height = 15,
                NumberOfMines = 30,
                StartTimeSeconds = 120,
                TeamSize = 1,
                WinCondition = WinConditions.TimeRush,
                HasPowerUps = false
            };

            var result = await _controller.PostGameSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        }

        [Test]
        public async Task PostGameSettingAsync_WithInvalidDimensions_ReturnsBadRequest()
        {
            var dto = new GameSettingDto
            {
                Width = 5,
                Height = 15,
                NumberOfMines = 10,
                StartTimeSeconds = 120,
                TeamSize = 1,
                WinCondition = WinConditions.TimeRush,
                HasPowerUps = false
            };

            var result = await _controller.PostGameSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetOrCreateSettingAsync_ExistingSetting_ReturnsOk()
        {
            var dto = new GameSettingDto
            {
                Width = 20,
                Height = 20,
                NumberOfMines = 50,
                StartTimeSeconds = 300,
                TeamSize = 1,
                WinCondition = WinConditions.TimeRush,
                HasPowerUps = false
            };

            var result = await _controller.GetOrCreateSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task GetOrCreateSettingAsync_NewSetting_ReturnsCreated()
        {
            var dto = new GameSettingDto
            {
                Width = 25,
                Height = 25,
                NumberOfMines = 60,
                StartTimeSeconds = 400,
                TeamSize = 2,
                WinCondition = WinConditions.TimeRush,
                HasPowerUps = true
            };

            var result = await _controller.GetOrCreateSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        }

        [Test]
        public async Task GetStandardModesAsync_ReturnsModes()
        {
            var result = await _controller.GetStandardModesAsync();

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var modes = okResult.Value as IEnumerable<GameSettingDto>;
            Assert.That(modes, Is.Not.Null);
            Assert.That(modes.Count(), Is.EqualTo(1));
        }
    }
}