using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.Repositories.AsyncImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Repositories
{
    [TestFixture]
    public class UserStatsRepositoryTests
    {
        private ApplicationDbContext _context;
        private UserStatsRepository _repository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new UserStatsRepository(_context);

            // Seed initial data
            var user1 = new User { UsersId = 1, Username = "Player1", Email = "p1@test.com", PasswordHash = new string('a', 60) };
            var user2 = new User { UsersId = 2, Username = "Player2", Email = "p2@test.com", PasswordHash = new string('b', 60) };
            var setting1 = new GameSetting { GameSettingsId = 10, Width = 10, Height = 10, NumberOfMines = 10 };

            _context.Users.AddRange(user1, user2);
            _context.GameSettings.Add(setting1);

            _context.UserStats.Add(new UserStats
            {
                UserId = 1,
                GameSettingId = 10,
                IsRanked = true,
                GamesPlayed = 10,
                Wins = 7,
                Losses = 3,
                PlayTime = 1200
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
        public async Task GetAllAsync_ReturnsAllUserStats()
        {
            var result = await _repository.GetAllAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetStatAsync_WithValidKeys_ReturnsCorrectStat()
        {
            var result = await _repository.GetStatAsync(1, 10, true);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.GamesPlayed, Is.EqualTo(10));
            Assert.That(result.Wins, Is.EqualTo(7));
        }

        [Test]
        public void GetStatAsync_WithInvalidUserId_ThrowsKeyNotFoundException()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _repository.GetStatAsync(999, 10, true));
        }

        [Test]
        public void Add_WithValidForeignKeys_AddsEntity()
        {
            var newStat = new UserStats
            {
                UserId = 2,
                GameSettingId = 10,
                IsRanked = false,
                GamesPlayed = 0,
                Wins = 0,
                Losses = 0,
                PlayTime = 0
            };

            _repository.Add(newStat);
            _repository.SaveChangesAsync().Wait();

            var dbEntity = _context.UserStats.FirstOrDefault(s => s.UserId == 2 && s.GameSettingId == 10);
            Assert.That(dbEntity, Is.Not.Null);
        }

        [Test]
        public void Add_WithInvalidUserId_ThrowsKeyNotFoundException()
        {
            var invalidStat = new UserStats
            {
                UserId = 999,
                GameSettingId = 10,
                IsRanked = false
            };

            Assert.Throws<KeyNotFoundException>(() => _repository.Add(invalidStat));
        }

        [Test]
        public async Task GetStatsWithLoadedPropertiesAsync_IncludesNavigationProperties()
        {
            var result = await _repository.GetStatsWithLoadedPropertiesAsync(1, 10, true);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.User, Is.Not.Null);
            Assert.That(result.User.Username, Is.EqualTo("Player1"));
            Assert.That(result.GameSetting, Is.Not.Null);
        }

        [Test]
        public void GetAsync_ThrowsNotImplementedException()
        {
            Assert.ThrowsAsync<NotImplementedException>(async () => await _repository.GetAsync(1));
        }

        [Test]
        public void Delete_RemovesEntityFromDatabase()
        {
            var stat = _context.UserStats.First();

            _repository.Delete(stat);
            _repository.SaveChangesAsync().Wait();

            Assert.That(_context.UserStats.Count(), Is.EqualTo(0));
        }
    }
}