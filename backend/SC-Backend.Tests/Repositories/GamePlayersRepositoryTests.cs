using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.Repositories;
using SC_Backend.Repositories.AsyncImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Repositories
{
    [TestFixture]
    public class GamePlayersRepositoryTests
    {
        private ApplicationDbContext _context;
        private GamePlayerRepository _repository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _repository = new GamePlayerRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region GetUserMatchHistoryAsync Tests

        [Test]
        public async Task GetUserMatchHistoryAsync_FiltersFinishedGamesOrdersByEndTimeDescAndPaginates()
        {
            var user = new User { UsersId = 1, Username = "Player1", Email = "p1@test.com", PasswordHash = "hash" };
            var otherUser = new User { UsersId = 2, Username = "Player2", Email = "p2@test.com", PasswordHash = "hash" };
            _context.Users.AddRange(user, otherUser);

            var now = DateTime.UtcNow;

            var oldFinishedGame = new Game { GamesId = 10, EndTime = now.AddHours(-3) };
            var midFinishedGame = new Game { GamesId = 11, EndTime = now.AddHours(-2) };
            var newFinishedGame = new Game { GamesId = 12, EndTime = now.AddHours(-1) };
            var unfinishedGame = new Game { GamesId = 13, EndTime = null };

            _context.Games.AddRange(oldFinishedGame, midFinishedGame, newFinishedGame, unfinishedGame);

            _context.GamePlayers.AddRange(
                new GamePlayer { PlayerId = 1, GameId = 10 },
                new GamePlayer { PlayerId = 1, GameId = 11 },
                new GamePlayer { PlayerId = 1, GameId = 12 },
                new GamePlayer { PlayerId = 1, GameId = 13 }, // Should be excluded (unfinished)
                new GamePlayer { PlayerId = 2, GameId = 12 }  // Should be excluded (other user)
            );

            await _context.SaveChangesAsync();

            // Page 1: PageSize 2 (Should get newest finished games: ID 12 and ID 11)
            var page1 = (await _repository.GetUserMatchHistoryAsync(userId: 1, page: 1, pageSize: 2)).ToList();

            Assert.That(page1.Count, Is.EqualTo(2));
            Assert.That(page1[0].GameId, Is.EqualTo(12));
            Assert.That(page1[0].Game, Is.Not.Null); // Checks .Include(gp => gp.Game) works
            Assert.That(page1[1].GameId, Is.EqualTo(11));

            // Page 2: PageSize 2 (Should get remaining finished game: ID 10)
            var page2 = (await _repository.GetUserMatchHistoryAsync(userId: 1, page: 2, pageSize: 2)).ToList();

            Assert.That(page2.Count, Is.EqualTo(1));
            Assert.That(page2[0].GameId, Is.EqualTo(10));
        }

        [Test]
        public async Task GetUserMatchHistoryAsync_WhenNoMatchesExist_ReturnsEmptyList()
        {
            var result = await _repository.GetUserMatchHistoryAsync(userId: 999, page: 1, pageSize: 10);

            Assert.That(result, Is.Empty);
        }

        #endregion

        #region UpdatePlayerResultsAsync Tests

        [Test]
        public async Task UpdatePlayerResultsAsync_UpdatesMultiplePlayerStatsInDatabase()
        {
            var player1 = new GamePlayer { GamePlayersId = 1, PlayerId = 1, GameId = 10, Score = 0, Outcome = Outcomes.Pending };
            var player2 = new GamePlayer { GamePlayersId = 2, PlayerId = 2, GameId = 10, Score = 0, Outcome = Outcomes.Pending };

            _context.GamePlayers.AddRange(player1, player2);
            await _context.SaveChangesAsync();

            // Clear tracker to simulate detached entity scenario
            _context.ChangeTracker.Clear();

            player1.Score = 150;
            player1.Outcome = Outcomes.Win;
            player1.EloChange = 15;

            player2.Score = 50;
            player2.Outcome = Outcomes.Loss;
            player2.EloChange = -12;

            await _repository.UpdatePlayerResultsAsync(new[] { player1, player2 });

            var updatedP1 = await _context.GamePlayers.AsNoTracking().FirstOrDefaultAsync(gp => gp.GamePlayersId == 1);
            var updatedP2 = await _context.GamePlayers.AsNoTracking().FirstOrDefaultAsync(gp => gp.GamePlayersId == 2);

            Assert.That(updatedP1, Is.Not.Null);
            Assert.That(updatedP1!.Score, Is.EqualTo(150));
            Assert.That(updatedP1.Outcome, Is.EqualTo(Outcomes.Win));
            Assert.That(updatedP1.EloChange, Is.EqualTo(15));

            Assert.That(updatedP2, Is.Not.Null);
            Assert.That(updatedP2!.Score, Is.EqualTo(50));
            Assert.That(updatedP2.Outcome, Is.EqualTo(Outcomes.Loss));
            Assert.That(updatedP2.EloChange, Is.EqualTo(-12));
        }

        #endregion

        #region GetTotalScoreForUserAsync Tests

        [Test]
        public async Task GetTotalScoreForUserAsync_CalculatesSumOfScoresOnlyForTargetUser()
        {
            _context.GamePlayers.AddRange(
                new GamePlayer { PlayerId = 1, GameId = 10, Score = 120 },
                new GamePlayer { PlayerId = 1, GameId = 11, Score = 80 },
                new GamePlayer { PlayerId = 1, GameId = 12, Score = 50 },
                new GamePlayer { PlayerId = 2, GameId = 10, Score = 300 } // Other user
            );

            await _context.SaveChangesAsync();

            var totalScore = await _repository.GetTotalScoreForUserAsync(userId: 1);

            Assert.That(totalScore, Is.EqualTo(250));
        }

        [Test]
        public async Task GetTotalScoreForUserAsync_UserHasNoGames_ReturnsZero()
        {
            var totalScore = await _repository.GetTotalScoreForUserAsync(userId: 999);

            Assert.That(totalScore, Is.EqualTo(0));
        }

        #endregion
    }
}