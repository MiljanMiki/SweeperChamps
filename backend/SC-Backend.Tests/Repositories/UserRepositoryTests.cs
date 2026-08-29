using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SC_Backend.DataContext;
using SC.Domain.DataModels;
using SC_Backend.Repositories.AsyncImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SC_Backend.Tests.Repositories
{
    [TestFixture]
    public class UserRepositoryTests
    {
        private ApplicationDbContext _context;
        private UserRepository _repository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new UserRepository(_context);

            // Seed sample users
            _context.Users.AddRange(
                new User
                {
                    UsersId = 1,
                    Username = "AlphaUser",
                    Email = "alpha@test.com",
                    PasswordHash = new string('a', 60),
                    Datecreated = new DateOnly(2025, 1, 1),
                    Elo = 1500,
                    UserRole = UserRoles.User
                },
                new User
                {
                    UsersId = 2,
                    Username = "BetaAdmin",
                    Email = "admin@test.com",
                    PasswordHash = new string('b', 60),
                    Datecreated = new DateOnly(2026, 5, 10),
                    Elo = 2100,
                    UserRole = UserRoles.Admin
                },
                new User
                {
                    UsersId = 3,
                    Username = "UnsetRoleUser",
                    Email = "unset@test.com",
                    PasswordHash = new string('c', 60),
                    Datecreated = new DateOnly(2026, 6, 1),
                    Elo = null,
                    UserRole = UserRoles.NotSet
                }
            );

            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetAsync_WithValidId_ReturnsUser()
        {
            var user = await _repository.GetAsync(1);

            Assert.That(user, Is.Not.Null);
            Assert.That(user!.Username, Is.EqualTo("AlphaUser"));
        }

        [Test]
        public async Task GetAsync_WithNonExistentId_ReturnsNull()
        {
            var user = await _repository.GetAsync(999);

            Assert.That(user, Is.Null);
        }

        [Test]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            var users = await _repository.GetAllAsync();

            Assert.That(users, Is.Not.Null);
            Assert.That(users.Count(), Is.EqualTo(3));
        }

        [Test]
        public async Task IsUniqueUsernameOrEmailAsync_WithExistingCredentials_ReturnsFalse()
        {
            var result = await _repository.IsUniqueUsernameOrEmailAsync("AlphaUser", "newemail@test.com");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsUniqueUsernameOrEmailAsync_WithNewCredentials_ReturnsTrue()
        {
            var result = await _repository.IsUniqueUsernameOrEmailAsync("BrandNewUser", "unique@test.com");

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task GetUserByUsername_WithValidUsername_ReturnsUser()
        {
            var user = await _repository.GetUserByUsernameAsync("BetaAdmin");

            Assert.That(user, Is.Not.Null);
            Assert.That(user!.Email, Is.EqualTo("admin@test.com"));
        }

        [Test]
        public async Task GetUserByEmail_WithValidEmail_ReturnsUser()
        {
            var user = await _repository.GetUserByEmailAsync("alpha@test.com");

            Assert.That(user, Is.Not.Null);
            Assert.That(user!.Username, Is.EqualTo("AlphaUser"));
        }

        [Test]
        public async Task GetLeaderboardAsync_ReturnsUsersSortedByEloDescending()
        {
            var leaderboard = (await _repository.GetLeaderboardAsync(10)).ToList();

            Assert.That(leaderboard.Count, Is.EqualTo(2)); // Excludes null Elo user
            Assert.That(leaderboard[0].Elo, Is.EqualTo((short)2100));
            Assert.That(leaderboard[1].Elo, Is.EqualTo((short)1500));
        }

        [Test]
        public async Task GetUserWithLoadedProperties_IncludesRequestedNavigations()
        {
            var user = await _repository.GetUserWithLoadedPropertiesAsync(1, history: true, stats: true);

            Assert.That(user, Is.Not.Null);
            Assert.That(user!.GamePlayers, Is.Not.Null);
            Assert.That(user.UserStats, Is.Not.Null);
        }

        [Test]
        public void FilterUsersAsync_WhenAllArgumentsNull_ThrowsArgumentNullException()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _repository.FilterUsersAsync(null, null, null, null, null));
        }

        [Test]
        public async Task FilterUsersAsync_ByRole_ReturnsMatchingUsers()
        {
            var result = await _repository.FilterUsersAsync(null, null, null, null, UserRoles.Admin);

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().Username, Is.EqualTo("BetaAdmin"));
        }

        [Test]
        public void Add_WithNullEntity_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _repository.Add(null!));
        }

        [Test]
        public async Task Add_And_SaveChangesAsync_PersistsUser()
        {
            var newUser = new User
            {
                UsersId = 4,
                Username = "NewMember",
                Email = "member@test.com",
                PasswordHash = new string('d', 60),
                Datecreated = new DateOnly(2026, 8, 1),
                UserRole = UserRoles.User
            };

            _repository.Add(newUser);
            await _repository.SaveChangesAsync();

            var dbUser = await _context.Users.FindAsync(4);
            Assert.That(dbUser, Is.Not.Null);
            Assert.That(dbUser!.Username, Is.EqualTo("NewMember"));
        }
    }
}