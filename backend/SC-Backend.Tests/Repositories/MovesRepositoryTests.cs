using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Repositories
{
    [TestFixture]
    public class MovesRepositoryTests
    {
        private ApplicationDbContext _context;
        private MovesRepository _repository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new MovesRepository(_context);

            // Seed required parent Game records and initial Move data
            var game1 = new Game { GamesId = 1, StartTime = DateTime.UtcNow };
            var game2 = new Game { GamesId = 2, StartTime = DateTime.UtcNow };

            _context.Games.AddRange(game1, game2);

            _context.Moves.Add(new Move
            {
                MovesId = 100,
                GameId = 1,
                MoveLog = "[{\"action\":\"reveal\",\"x\":1,\"y\":2}]"
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
        public async Task GetAsync_WithValidId_ReturnsMove()
        {
            var move = await _repository.GetAsync(100);

            Assert.That(move, Is.Not.Null);
            Assert.That(move!.GameId, Is.EqualTo(1));
            Assert.That(move.MoveLog, Is.EqualTo("[{\"action\":\"reveal\",\"x\":1,\"y\":2}]"));
        }

        [Test]
        public async Task GetAsync_WithNonExistentId_ReturnsNull()
        {
            var move = await _repository.GetAsync(999);

            Assert.That(move, Is.Null);
        }

        [Test]
        public async Task GetAllAsync_ReturnsAllMoves()
        {
            var moves = await _repository.GetAllAsync();

            Assert.That(moves, Is.Not.Null);
            Assert.That(moves.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Add_WithNullEntity_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _repository.Add(null!));
        }

        [Test]
        public void Add_WithInvalidGameId_ThrowsKeyNotFoundException()
        {
            var invalidMove = new Move
            {
                MovesId = 101,
                GameId = 999, // GameId 999 does not exist in Games
                MoveLog = "[]"
            };

            Assert.Throws<KeyNotFoundException>(() => _repository.Add(invalidMove));
        }

        [Test]
        public async Task Add_WithValidGameId_PersistsMove()
        {
            var newMove = new Move
            {
                MovesId = 101,
                GameId = 2,
                MoveLog = "[{\"action\":\"flag\",\"x\":5,\"y\":5}]"
            };

            _repository.Add(newMove);
            await _repository.SaveChangesAsync();

            var dbMove = await _context.Moves.FindAsync(101);
            Assert.That(dbMove, Is.Not.Null);
            Assert.That(dbMove!.GameId, Is.EqualTo(2));
        }

        [Test]
        public void Delete_WithNullEntity_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _repository.Delete(null!));
        }

        [Test]
        public async Task Delete_RemovesMoveFromDatabase()
        {
            var move = await _context.Moves.FindAsync(100);
            Assert.That(move, Is.Not.Null);

            _repository.Delete(move!);
            await _repository.SaveChangesAsync();

            var dbMove = await _context.Moves.FindAsync(100);
            Assert.That(dbMove, Is.Null);
        }

        [Test]
        public void Update_WithNullEntity_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _repository.Update(null!));
        }

        [Test]
        public async Task Update_ModifiesExistingMove()
        {
            var move = await _context.Moves.FindAsync(100);
            Assert.That(move, Is.Not.Null);

            move!.MoveLog = "[{\"action\":\"reveal\",\"x\":1,\"y\":2},{\"action\":\"flag\",\"x\":0,\"y\":0}]";
            _repository.Update(move);
            await _repository.SaveChangesAsync();

            var updatedMove = await _context.Moves.FindAsync(100);
            Assert.That(updatedMove!.MoveLog, Contains.Substring("flag"));
        }

        [Test]
        public async Task GetByGameIdAsync_WithExistingGameId_ReturnsMove()
        {
            var move = await _repository.GetByGameIdAsync(1);

            Assert.That(move, Is.Not.Null);
            Assert.That(move!.MovesId, Is.EqualTo(100));
            Assert.That(move.GameId, Is.EqualTo(1));
        }

        [Test]
        public async Task GetByGameIdAsync_WithNonExistentGameId_ReturnsNull()
        {
            var move = await _repository.GetByGameIdAsync(999);

            Assert.That(move, Is.Null);
        }

        [Test]
        public async Task HasMovesForGameAsync_WhenMovesExist_ReturnsTrue()
        {
            var hasMoves = await _repository.HasMovesForGameAsync(1);

            Assert.That(hasMoves, Is.True);
        }

        [Test]
        public async Task HasMovesForGameAsync_WhenNoMovesExist_ReturnsFalse()
        {
            var hasMoves = await _repository.HasMovesForGameAsync(2);

            Assert.That(hasMoves, Is.False);
        }

        [Test]
        [Ignore("Unit test doesnt work since ExecuteDeleteAsync demands a relational database to be set up properly" +
            "in order to work.")]
        public async Task DeleteByGameIdAsync_RemovesMatchingMoves()
        {
            await _repository.DeleteByGameIdAsync(1);

            var dbMove = await _context.Moves.FindAsync(100);
            Assert.That(dbMove, Is.Null);
        }
    }
}