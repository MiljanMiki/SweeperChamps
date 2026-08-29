using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SC.Api.Controllers;
using SC_Backend.DataModels;
using SC_Backend.DTOs.Users;
using SC_Backend.Repositories.AsyncInterfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Controllers
{
    [TestFixture]
    public class UsersControllerTests
    {
        private Mock<IUserRepository> _mockRepo;
        private UsersController _controller;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new Mock<IUserRepository>();
            _controller = new UsersController(_mockRepo.Object);
        }

        [Test]
        public async Task GetUsersAsync_ReturnsOkWithUserDtoList()
        {
            var users = new List<User>
            {
                new User { UsersId = 1, Username = "User1", Email = "u1@test.com", UserRole = UserRoles.User }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

            var result = await _controller.GetUsersAsync();

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            var dtos = okResult.Value as IEnumerable<UserDTO>;
            Assert.That(dtos, Is.Not.Null);
            Assert.That(dtos.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetUserAsync_WithInvalidId_ReturnsBadRequest()
        {
            var result = await _controller.GetUserAsync(0);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetUserAsync_WhenNotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetAsync(999)).ReturnsAsync((User?)null);

            var result = await _controller.GetUserAsync(999);

            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetUserAsync_WithValidId_ReturnsOkWithDto()
        {
            var user = new User { UsersId = 1, Username = "User1", Email = "u1@test.com" };
            _mockRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(user);

            var result = await _controller.GetUserAsync(1);

            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            var dto = okResult.Value as UserDTO;
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.Username, Is.EqualTo("User1"));
        }

        [Test]
        public async Task PutUserAsync_WithInvalidId_ReturnsBadRequest()
        {
            var dto = new UserUpdateDTO { Username = "Test", Email = "test@test.com", UserRole = UserRoles.User };

            var result = await _controller.PutUserAsync(0, dto);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PutUserAsync_WithNullOrWhitespaceStrings_ReturnsBadRequest()
        {
            var dto = new UserUpdateDTO { Username = "", Email = "test@test.com", UserRole = UserRoles.User };

            var result = await _controller.PutUserAsync(1, dto);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PutUserAsync_WithValidData_ReturnsNoContent()
        {
            var existingUser = new User { UsersId = 1, Username = "OldName", Email = "old@test.com", Elo = null };
            var dto = new UserUpdateDTO { Username = "NewName", Email = "new@test.com", UserRole = UserRoles.User, Elo = 1000 };

            _mockRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(existingUser);
            _mockRepo.Setup(r => r.IsUniqueUsernameOrEmailAsync(dto.Username, dto.Email)).ReturnsAsync(true);

            var result = await _controller.PutUserAsync(1, dto);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task DeleteUserAsync_WithInvalidId_ReturnsBadRequest()
        {
            var result = await _controller.DeleteUserAsync(-1);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task DeleteUserAsync_WhenNotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetAsync(999)).ReturnsAsync((User?)null);

            var result = await _controller.DeleteUserAsync(999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task DeleteUserAsync_WhenFound_DeletesAndReturnsNoContent()
        {
            var user = new User { UsersId = 1, Username = "ToDelete", Email = "delete@test.com" };
            _mockRepo.Setup(r => r.GetAsync(1)).ReturnsAsync(user);

            var result = await _controller.DeleteUserAsync(1);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
            _mockRepo.Verify(r => r.Delete(user), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task GetLeaderboard_WithInvalidCount_ReturnsBadRequest()
        {
            var result = await _controller.GetLeaderboard(0);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetLeaderboard_WithValidCount_ReturnsOk()
        {
            var leaderboard = new List<User> { new User { Username = "Top1", Elo = 2000 } };
            _mockRepo.Setup(r => r.GetLeaderboardAsync(5)).ReturnsAsync(leaderboard);

            var result = await _controller.GetLeaderboard(5);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }
    }
}