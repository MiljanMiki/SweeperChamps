using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SC_Backend.Controllers;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.GameSettings;

namespace SC_Backend.Tests.Controllers
{
    /// <summary>
    /// NUnit tests for GameSettingsController.
    ///
    /// ASSUMPTIONS (ApplicationDbContext.cs and GameSettingDto.cs were not part of the uploaded files):
    ///  - ApplicationDbContext exposes a public constructor accepting DbContextOptions&lt;ApplicationDbContext&gt;
    ///    and a DbSet&lt;GameSetting&gt; GameSettings property, following standard EF Core scaffolding conventions.
    ///  - GameSettingDto is a plain DTO mirroring GameSetting's public properties 1:1
    ///    (GameSettingsId, Width, Height, NumberOfMines, StartTimeSeconds, TeamSize, WinCondition, HasPowerUps),
    ///    which is what MapToDto/PostGameSettingAsync/GetOrCreateSettingAsync imply.
    /// If either differs in the real project, adjust SetUp()/the DTO builders below accordingly.
    ///
    /// Uses the EF Core InMemory provider (Microsoft.EntityFrameworkCore.InMemory) instead of mocking
    /// DbContext/DbSet, since the controller relies on LINQ-to-Entities (FirstOrDefaultAsync, Where, OrderBy)
    /// which is impractical to fake faithfully with a mocking library.
    ///
    /// NuGet packages needed in the test project: NUnit, NUnit3TestAdapter, Microsoft.NET.Test.Sdk,
    /// Microsoft.EntityFrameworkCore.InMemory, plus a project reference to SC_Backend.
    /// </summary>
    [TestFixture]
    public class GameSettingsControllerTests
    {
        // Mirrors the private constants in GameSettingsController. They're duplicated here because the
        // controller keeps them private with no test hook - consider exposing them (internal + InternalsVisibleTo,
        // or configuration) so these numbers can't silently drift from the real limits.
        private const int MinHeight = 10, MaxHeight = 50, MinWidth = 10, MaxWidth = 50;

        private ApplicationDbContext _context = null!;
        private GameSettingsController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new GameSettingsController(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        private static GameSettingDto ValidRaceDto(
            int width = 20, int height = 20, int mines = 40, int teamSize = 2, bool powerUps = false) =>
            new GameSettingDto
            {
                Width = width,
                Height = height,
                NumberOfMines = mines,
                StartTimeSeconds = null,
                TeamSize = teamSize,
                WinCondition = WinConditions.Race,
                HasPowerUps = powerUps
            };

        private static GameSettingDto ValidTimeRushDto(
            int width = 20, int height = 20, int mines = 40, int teamSize = 2, int startTime = 300, bool powerUps = false) =>
            new GameSettingDto
            {
                Width = width,
                Height = height,
                NumberOfMines = mines,
                StartTimeSeconds = startTime,
                TeamSize = teamSize,
                WinCondition = WinConditions.TimeRush,
                HasPowerUps = powerUps
            };

        private async Task<GameSetting> SeedSettingAsync(GameSettingDto dto)
        {
            var entity = new GameSetting
            {
                Width = dto.Width,
                Height = dto.Height,
                NumberOfMines = dto.NumberOfMines,
                StartTimeSeconds = dto.StartTimeSeconds,
                TeamSize = dto.TeamSize,
                WinCondition = dto.WinCondition,
                HasPowerUps = dto.HasPowerUps
            };
            _context.GameSettings.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        #region GET (all)

        [Test]
        public async Task GetGameSettingsAsync_NoSettings_ReturnsEmptyList()
        {
            var result = await _controller.GetGameSettingsAsync();

            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value, Is.Empty);
        }

        [Test]
        public async Task GetGameSettingsAsync_WithSettings_ReturnsAllMappedToDto()
        {
            await SeedSettingAsync(ValidRaceDto());
            await SeedSettingAsync(ValidTimeRushDto());

            var result = await _controller.GetGameSettingsAsync();

            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.Count(), Is.EqualTo(2));
            Assert.That(result.Value!.All(d => d.GameSettingsId > 0), Is.True);
        }

        #endregion

        #region GET (by id)

        [Test]
        public async Task GetGameSettingAsync_IdZero_ReturnsBadRequest()
        {
            var result = await _controller.GetGameSettingAsync(0);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetGameSettingAsync_NegativeId_ReturnsBadRequest()
        {
            var result = await _controller.GetGameSettingAsync(-5);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetGameSettingAsync_UnknownId_ReturnsNotFound()
        {
            var result = await _controller.GetGameSettingAsync(12345);

            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task GetGameSettingAsync_KnownId_ReturnsMatchingDto()
        {
            var entity = await SeedSettingAsync(ValidTimeRushDto(width: 15, height: 15, mines: 30));

            var result = await _controller.GetGameSettingAsync(entity.GameSettingsId);

            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value!.GameSettingsId, Is.EqualTo(entity.GameSettingsId));
            Assert.That(result.Value!.Width, Is.EqualTo(15));
            Assert.That(result.Value!.Height, Is.EqualTo(15));
            Assert.That(result.Value!.NumberOfMines, Is.EqualTo(30));
        }

        #endregion

        #region POST (create)

        [Test]
        public async Task PostGameSettingAsync_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.PostGameSettingAsync(null!);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PostGameSettingAsync_ValidDto_PersistsAndReturnsCreatedAtAction()
        {
            var dto = ValidRaceDto();

            var result = await _controller.PostGameSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
            var created = (CreatedAtActionResult)result.Result!;
            Assert.That(created.ActionName, Is.EqualTo(nameof(GameSettingsController.GetGameSettingAsync)));
            Assert.That(_context.GameSettings.Count(), Is.EqualTo(1));

            var savedEntity = (GameSetting)created.Value!;
            Assert.That(created.RouteValues, Is.Not.Null);
            Assert.That(created.RouteValues!["id"], Is.EqualTo(savedEntity.GameSettingsId));
        }

        [Test]
        public async Task PostGameSettingAsync_ReturnsRawEntity_NotDto_InconsistentWithFindOrCreate()
        {
            // Documents an inconsistency: PostGameSettingAsync hands back the EF entity (GameSetting)
            // while find-or-create and every GET action hand back GameSettingDto. That leaks the
            // persistence shape and serializes WinCondition as a raw int instead of the enum name a
            // client gets from every other endpoint.
            var dto = ValidRaceDto();

            var result = await _controller.PostGameSettingAsync(dto);

            var created = (CreatedAtActionResult)result.Result!;
            Assert.That(created.Value, Is.InstanceOf<GameSetting>());
        }

        [TestCase(9)]   // below minHeight
        [TestCase(51)]  // above maxHeight
        public async Task PostGameSettingAsync_HeightOutOfRange_ReturnsBadRequest(int height)
        {
            var dto = ValidRaceDto(height: height);

            var result = await _controller.PostGameSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [TestCase(9)]   // below minWidth
        [TestCase(51)]  // above maxWidth
        public async Task PostGameSettingAsync_WidthOutOfRange_ReturnsBadRequest(int width)
        {
            var dto = ValidRaceDto(width: width);

            var result = await _controller.PostGameSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PostGameSettingAsync_WidthOutOfRange_ErrorMessageIncorrectlySaysHeight_DocumentsBug()
        {
            // Copy-paste bug in CheckDto: the width branch's message still says "Height".
            var dto = ValidRaceDto(width: 5); // valid height, invalid width

            var result = await _controller.PostGameSettingAsync(dto);

            var badRequest = (BadRequestObjectResult)result.Result!;
            // This currently PASSES, proving the message is wrong: a caller debugging a width
            // problem is told to go fix "Height" instead.
            Assert.That(badRequest.Value!.ToString(), Does.Contain("Height"));
            Assert.That(badRequest.Value!.ToString(), Does.Not.Contain("Width"));
        }

        [Test]
        public async Task PostGameSettingAsync_MinesEqualToBoardSize_ReturnsBadRequest()
        {
            var dto = ValidRaceDto(width: 10, height: 10, mines: 100); // boundary: 100 == 10*10

            var result = await _controller.PostGameSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PostGameSettingAsync_MinesExceedBoardSize_ReturnsBadRequest()
        {
            var dto = ValidRaceDto(width: 10, height: 10, mines: 500);

            var result = await _controller.PostGameSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PostGameSettingAsync_NegativeMines_IsIncorrectlyAccepted_DocumentsBug()
        {
            // CheckDto only rejects NumberOfMines >= Width * Height; it never rejects negative counts,
            // so a nonsensical "-5 mines" setting is accepted and persisted.
            var dto = ValidRaceDto(mines: -5);

            var result = await _controller.PostGameSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>(),
                "This currently succeeds, which demonstrates the missing validation.");
        }

        [Test]
        public async Task PostGameSettingAsync_RaceModeWithStartTime_ReturnsBadRequest()
        {
            var dto = ValidRaceDto();
            dto.StartTimeSeconds = 60;

            var result = await _controller.PostGameSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PostGameSettingAsync_TimeRushWithoutStartTime_ReturnsBadRequest()
        {
            var dto = ValidTimeRushDto();
            dto.StartTimeSeconds = null;

            var result = await _controller.PostGameSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [TestCase(0)]
        [TestCase(-1)]
        public async Task PostGameSettingAsync_TeamSizeNotPositive_ReturnsBadRequest(int teamSize)
        {
            var dto = ValidRaceDto(teamSize: teamSize);

            var result = await _controller.PostGameSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PostGameSettingAsync_DuplicateSettings_AreBothPersisted_NoUniquenessEnforced()
        {
            // Unlike find-or-create, plain POST never checks for an existing identical row,
            // so calling it twice with the same dto creates two rows with different ids.
            var dto = ValidRaceDto();

            await _controller.PostGameSettingAsync(dto);
            await _controller.PostGameSettingAsync(dto);

            Assert.That(_context.GameSettings.Count(), Is.EqualTo(2));
        }

        #endregion

        #region find-or-create

        [Test]
        public void GetOrCreateSettingAsync_NullDto_ThrowsNullReferenceException_DocumentsBug()
        {
            // Unlike PostGameSettingAsync, this action never null-checks dto before calling CheckDto(dto),
            // so a missing/empty request body crashes with an unhandled 500 instead of a clean 400.
            Assert.ThrowsAsync<NullReferenceException>(async () => await _controller.GetOrCreateSettingAsync(null!));
        }

        [Test]
        public async Task GetOrCreateSettingAsync_InvalidDto_ReturnsBadRequest()
        {
            var dto = ValidRaceDto(height: 5);

            var result = await _controller.GetOrCreateSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetOrCreateSettingAsync_NoMatch_CreatesNewSettingAndReturnsDto()
        {
            var dto = ValidRaceDto();

            var result = await _controller.GetOrCreateSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
            Assert.That(_context.GameSettings.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetOrCreateSettingAsync_ExactMatchExists_ReturnsExistingWithoutDuplicating()
        {
            var seeded = await SeedSettingAsync(ValidRaceDto());
            var dto = ValidRaceDto(); // identical values

            var result = await _controller.GetOrCreateSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            var returnedDto = (GameSettingDto)ok.Value!;
            Assert.That(returnedDto.GameSettingsId, Is.EqualTo(seeded.GameSettingsId));
            Assert.That(_context.GameSettings.Count(), Is.EqualTo(1), "No duplicate row should have been created.");
        }

        [Test]
        public async Task GetOrCreateSettingAsync_DifferentByOneField_CreatesSeparateSetting()
        {
            await SeedSettingAsync(ValidRaceDto(teamSize: 2));
            var dto = ValidRaceDto(teamSize: 3); // only TeamSize differs

            var result = await _controller.GetOrCreateSettingAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
            Assert.That(_context.GameSettings.Count(), Is.EqualTo(2));
        }

        #endregion

        #region standard-modes

        [Test]
        public async Task GetStandardModesAsync_NoMatches_ReturnsEmptyList()
        {
            // Only settings with powerups / team size != 1 exist.
            await SeedSettingAsync(ValidRaceDto(teamSize: 2, powerUps: true));

            var result = await _controller.GetStandardModesAsync();

            var ok = (OkObjectResult)result.Result!;
            var list = (IEnumerable<GameSettingDto>)ok.Value!;
            Assert.That(list, Is.Empty);
        }

        [Test]
        public async Task GetStandardModesAsync_ExcludesPowerUpsAndNonSoloTeams()
        {
            await SeedSettingAsync(ValidRaceDto(width: 10, height: 10, teamSize: 1, powerUps: false)); // eligible
            await SeedSettingAsync(ValidRaceDto(width: 12, height: 12, teamSize: 1, powerUps: true));  // excluded: powerups
            await SeedSettingAsync(ValidRaceDto(width: 14, height: 14, teamSize: 2, powerUps: false)); // excluded: team size

            var result = await _controller.GetStandardModesAsync();

            var ok = (OkObjectResult)result.Result!;
            var list = ((IEnumerable<GameSettingDto>)ok.Value!).ToList();
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0].Width, Is.EqualTo(10));
        }

        [Test]
        public async Task GetStandardModesAsync_ReturnsAtMostThree_OrderedByBoardArea()
        {
            await SeedSettingAsync(ValidRaceDto(width: 20, height: 20, teamSize: 1)); // area 400
            await SeedSettingAsync(ValidRaceDto(width: 10, height: 10, teamSize: 1)); // area 100
            await SeedSettingAsync(ValidRaceDto(width: 16, height: 16, teamSize: 1)); // area 256
            await SeedSettingAsync(ValidRaceDto(width: 30, height: 30, teamSize: 1)); // area 900, should be excluded (4th)

            var result = await _controller.GetStandardModesAsync();

            var ok = (OkObjectResult)result.Result!;
            var list = ((IEnumerable<GameSettingDto>)ok.Value!).ToList();
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Select(d => d.Width * d.Height), Is.Ordered.Ascending);
        }

        #endregion
    }
}