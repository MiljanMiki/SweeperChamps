using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SC.Api.Controllers;
using SC.Domain.DataModels;
using SC.Domain.DTOs.UserStats;
using SC.Domain.Repositories.AsyncInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Controllers
{
    [TestFixture]
    public class UserStatsControllerTests
    {
        private Mock<IUserStatsRepository> _mockRepo;
        private UserStatsController _controller;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new Mock<IUserStatsRepository>();
            _controller = new UserStatsController(_mockRepo.Object);
        }

        [Test]
        public async Task GetUserStatsAsync_ReturnsOkWithStatsList()
        {
            var stats = new List<UserStats>
            {
                new UserStats { UserId = 1, GameSettingId = 10, IsRanked = true, GamesPlayed = 5, Wins = 3, Losses = 2, PlayTime = 300 }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(stats);

            var result = await _controller.GetUserStatsAsync();

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            var dtos = okResult.Value as IEnumerable<UserStatDTO>;
            Assert.That(dtos, Is.Not.Null);
            Assert.That(dtos.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetUserStatsAsync_WithInvalidIds_ReturnsBadRequest()
        {
            var result = await _controller.GetUserStatsAsync(0, 10, true);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetUserStatsAsync_WhenNotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetStatAsync(1, 10, true)).ReturnsAsync((UserStats?)null);

            var result = await _controller.GetUserStatsAsync(1, 10, true);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetUserStatsAsync_WithValidKeys_ReturnsOkWithDto()
        {
            var stat = new UserStats { UserId = 1, GameSettingId = 10, IsRanked = true, GamesPlayed = 5, Wins = 3, Losses = 2, PlayTime = 300 };
            _mockRepo.Setup(r => r.GetStatAsync(1, 10, true)).ReturnsAsync(stat);

            var result = await _controller.GetUserStatsAsync(1, 10, true);

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            var dto = okResult.Value as UserStatDTO;
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.Wins, Is.EqualTo(3));
        }

        [Test]
        public async Task PutUserStatsAsync_WithInvalidDto_ReturnsBadRequest()
        {
            var dto = new FullStatDTO { UserId = 1, GameSettingId = 10, GamesPlayed = 5, Wins = 10, Losses = 2 }; // Wins + Losses > GamesPlayed

            var result = await _controller.PutUserStatsAsync(dto);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PutUserStatsAsync_WhenStatNotFound_ReturnsNotFound()
        {
            var dto = new FullStatDTO { UserId = 1, GameSettingId = 10, IsRanked = true, GamesPlayed = 5, Wins = 3, Losses = 2, PlayTime = 100 };
            _mockRepo.Setup(r => r.GetStatAsync(1, 10, true)).ReturnsAsync((UserStats?)null);

            var result = await _controller.PutUserStatsAsync(dto);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task PostUserStatsAsync_WithValidDto_CreatesStat()
        {
            var dto = new FullStatDTO { UserId = 1, GameSettingId = 10, IsRanked = true, GamesPlayed = 0, Wins = 0, Losses = 0, PlayTime = 0 };
            _mockRepo.Setup(r => r.GetStatAsync(1, 10, true)).ReturnsAsync((UserStats?)null);

            var result = await _controller.PostUserStatsAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
            _mockRepo.Verify(r => r.Add(It.IsAny<UserStats>()), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task DeleteUserStatsAsync_WhenNotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetStatAsync(1, 10, true)).ReturnsAsync((UserStats?)null);

            var result = await _controller.DeleteUserStatsAsync(1, 10, true);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task DeleteUserStatsAsync_WhenFound_DeletesAndReturnsNoContent()
        {
            var stat = new UserStats { UserId = 1, GameSettingId = 10, IsRanked = true };
            _mockRepo.Setup(r => r.GetStatAsync(1, 10, true)).ReturnsAsync(stat);

            var result = await _controller.DeleteUserStatsAsync(1, 10, true);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            _mockRepo.Verify(r => r.Delete(stat), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task RecordGameEndingAsync_WithNegativeDuration_ReturnsBadRequest()
        {
            var result = await _controller.RecordGameEndingAsync(1, 10, true, true, -50);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task RecordGameEndingAsync_WithValidData_ReturnsNoContent()
        {
            var result = await _controller.RecordGameEndingAsync(1, 10, true, true, 120);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            _mockRepo.Verify(r => r.RecordMatchResultAsync(1, 10, true, true, 120), Times.Once);
        }
    }
}