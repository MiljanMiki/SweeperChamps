using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SC_Backend.DataContext;
using SC.Domain.DataModels;
using SC_Backend.Repositories;
using SC_Backend.Repositories.AsyncImplementations;
using SC_Backend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Repositories
{
    [TestFixture]
    public class GamesRepositoryTests
    {
        private ApplicationDbContext _context;
        private GameRepository _repository;
        private IAuthService _authService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new GameRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region GetLiveGamesAsync Tests

        [Test]
        public async Task GetLiveGamesAsync_ReturnsOnlyActiveGamesOrderedByStartTimeDescAndHonorsLimit()
        {
            var now = DateTime.UtcNow;

            var liveGameOld = new Game { GamesId = 1, StartTime = now.AddMinutes(-30), EndTime = null };
            var liveGameNew = new Game { GamesId = 2, StartTime = now.AddMinutes(-5), EndTime = null };
            var finishedGame = new Game { GamesId = 3, StartTime = now.AddMinutes(-60), EndTime = now.AddMinutes(-10) };

            _context.Games.AddRange(liveGameOld, liveGameNew, finishedGame);
            await _context.SaveChangesAsync();

            var result = (await _repository.GetLiveGamesAsync(limit: 1)).ToList();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().GamesId, Is.EqualTo(2)); // Newest live game first
        }

        [Test]
        public async Task GetLiveGamesAsync_WhenNoLiveGamesExist_ReturnsEmptyList()
        {
            var finishedGame = new Game { GamesId = 1, StartTime = DateTime.UtcNow.AddHours(-1), EndTime = DateTime.UtcNow };
            _context.Games.Add(finishedGame);
            await _context.SaveChangesAsync();

            var result = await _repository.GetLiveGamesAsync(10);

            Assert.That(result, Is.Empty);
        }

        #endregion

        #region MarkGameAsFinishedAsync Tests

        [Test]
        [Ignore("Cannot test ExecuteUpdateAsync method")]
        public async Task MarkGameAsFinishedAsync_ValidGameId_UpdatesEndTimeDurationAndWinningTeam()
        {
            var game = new Game
            {
                GamesId = 1,
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = null,
                DurationSeconds = null,
                WinningTeam = null
            };
            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            await _repository.MarkGameAsFinishedAsync(1, 600, TeamColors.Red);

            // Fetch fresh state from DB (bypassing EF change tracker)
            var updatedGame = await _context.Games.AsNoTracking().FirstOrDefaultAsync(g => g.GamesId == 1);

            Assert.That(updatedGame, Is.Not.Null);
            Assert.That(updatedGame!.EndTime, Is.Not.Null);
            Assert.That(updatedGame.DurationSeconds, Is.EqualTo(600));
            Assert.That(updatedGame.WinningTeam, Is.EqualTo(TeamColors.Red));
        }

        [Test]
        [Ignore("Cannot test ExecuteUpdateAsync method")]
        public async Task MarkGameAsFinishedAsync_InvalidGameId_DoesNotUpdateExistingGames()
        {
            var game = new Game { GamesId = 1, EndTime = null };
            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            await _repository.MarkGameAsFinishedAsync(999, 300, TeamColors.Blue);

            var dbGame = await _context.Games.AsNoTracking().FirstOrDefaultAsync(g => g.GamesId == 1);
            Assert.That(dbGame!.EndTime, Is.Null);
        }

        #endregion

        #region GetGamesWithPlayer Tests

        [Test]
        public void GetGamesWithPlayer_PlayerDoesNotExist_ThrowsKeyNotFoundException()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _repository.GetGamesWithPlayer(playerID: 999, limit: 10, ranked: true));
        }

        [Test]
        public async Task GetGamesWithPlayer_ValidPlayer_FiltersByRankedStatusAndHonorsLimit()
        {
            var user = new User { UsersId = 1, Email="asdf@gmail.com",PasswordHash= new string('a', 60), Username = "TestPlayer" };
            _context.Users.Add(user);

            var unrankedGame = new Game { GamesId = 10, IsRanked = false };
            var rankedGame1 = new Game { GamesId = 11, IsRanked = true };
            var rankedGame2 = new Game { GamesId = 12, IsRanked = true };

            _context.Games.AddRange(unrankedGame, rankedGame1, rankedGame2);

            _context.GamePlayers.AddRange(
                new GamePlayer { GameId = 10, PlayerId = 1 },
                new GamePlayer { GameId = 11, PlayerId = 1 },
                new GamePlayer { GameId = 12, PlayerId = 1 }
            );

            await _context.SaveChangesAsync();

            // Act 1: Get ranked games with limit 1
            var rankedResult = (await _repository.GetGamesWithPlayer(playerID: 1, limit: 1, ranked: true)).ToList();

            Assert.That(rankedResult.Count, Is.EqualTo(1));
            Assert.That(rankedResult.First().IsRanked, Is.True);

            // Act 2: Get unranked games
            var unrankedResult = (await _repository.GetGamesWithPlayer(playerID: 1, limit: 10, ranked: false)).ToList();

            Assert.That(unrankedResult.Count, Is.EqualTo(1));
            Assert.That(unrankedResult.First().GamesId, Is.EqualTo(10));
        }

        #endregion
    }
}