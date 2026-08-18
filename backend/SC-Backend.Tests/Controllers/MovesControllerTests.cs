using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SC_Backend.Controllers;
using SC_Backend.DataModels;
using SC_Backend.DTOs.Moves;
using SC_Backend.Repositories.AsyncInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Controllers
{
    [TestFixture]
    public class MovesControllerTests
    {
        private Mock<IMovesRepository> _mockRepo;
        private MovesController _controller;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new Mock<IMovesRepository>();
            _controller = new MovesController(_mockRepo.Object);
        }

        [Test]
        public async Task GetMovesAsync_ReturnsOkWithMoveDtos()
        {
            var moves = new List<Move>
            {
                new Move { MovesId = 1, GameId = 10, MoveLog = "[{\"x\":0,\"y\":0}]" }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(moves);

            var result = await _controller.GetMovesAsync();

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            var dtos = okResult.Value as IEnumerable<MoveDTO>;
            Assert.That(dtos, Is.Not.Null);
            Assert.That(dtos.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetMoveAsync_WithInvalidId_ReturnsBadRequest()
        {
            var result = await _controller.GetMoveAsync(0);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetMoveAsync_WhenNotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetAsync(1)).ReturnsAsync((Move?)null);

            var result = await _controller.GetMoveAsync(1);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetMoveAsync_WithValidId_ReturnsOkWithDto()
        {
            var move = new Move { MovesId = 1, GameId = 10, MoveLog = "[]" };
            _mockRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(move);

            var result = await _controller.GetMoveAsync(1);

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            var dto = okResult.Value as MoveDTO;
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.GameId, Is.EqualTo(10));
        }

        [Test]
        public async Task PutMoveAsync_WithInvalidId_ReturnsBadRequest()
        {
            var dto = new PutDTO { MoveLog = null };
            var result = await _controller.PutMoveAsync(0, dto);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PutMoveAsync_WithValidId_UpdatesAndReturnsNoContent()
        {
            var move = new Move { MovesId = 1, GameId = 10, MoveLog = "[]" };
            _mockRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(move);

            var dto = new PutDTO { MoveLog = "[{\"action\":\"reveal\"}]" };
            var result = await _controller.PutMoveAsync(1, dto);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            Assert.That(move.MoveLog, Is.EqualTo("[{\"action\":\"reveal\"}]"));
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task PostMoveAsync_WithInvalidGameId_ReturnsBadRequest()
        {
            var dto = new MoveDTO { GameId = 0, MoveLog = "[]" };

            var result = await _controller.PostMoveAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PostMoveAsync_WithValidDto_ReturnsCreatedAtAction()
        {
            var dto = new MoveDTO { GameId = 10, MoveLog = "[]" };

            var result = await _controller.PostMoveAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
            _mockRepo.Verify(r => r.Add(It.IsAny<Move>()), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task DeleteMoveAsync_WhenNotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetAsync(1)).ReturnsAsync((Move?)null);

            var result = await _controller.DeleteMoveAsync(1);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task DeleteMoveAsync_WhenFound_DeletesAndReturnsNoContent()
        {
            var move = new Move { MovesId = 1, GameId = 10 };
            _mockRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(move);

            var result = await _controller.DeleteMoveAsync(1);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            _mockRepo.Verify(r => r.Delete(move), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task GetByGameIdAsync_WhenFound_ReturnsOk()
        {
            var move = new Move { MovesId = 1, GameId = 10, MoveLog = "[]" };
            _mockRepo.Setup(r => r.GetByGameIdAsync(10)).ReturnsAsync(move);

            var result = await _controller.GetByGameIdAsync(10);

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
        }

        [Test]
        public async Task HasMovesForGameAsync_ReturnsBooleanResult()
        {
            _mockRepo.Setup(r => r.HasMovesForGameAsync(10)).ReturnsAsync(true);

            var result = await _controller.HasMovesForGameAsync(10);

            Assert.That(result.Value, Is.True);
        }
    }
}