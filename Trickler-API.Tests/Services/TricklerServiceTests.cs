using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Trickler_API.Data;
using Trickler_API.DTO;
using Trickler_API.Exceptions;
using Trickler_API.Models;
using Trickler_API.Services;

namespace Trickler_API.Tests.Services
{
    public class TricklerServiceTests : IDisposable
    {
        private readonly TricklerDbContext _context;
        private readonly Mock<ILogger<TricklerService>> _loggerMock;
        private readonly Mock<TimeProvider> _timeProviderMock;
        private readonly TricklerService _service;

        public TricklerServiceTests()
        {
            // Use InMemory database
            var options = new DbContextOptionsBuilder<TricklerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)) // transactions are not supported with in memory db
                .Options;

            _context = new TricklerDbContext(options);
            _loggerMock = new Mock<ILogger<TricklerService>>();
            _timeProviderMock = new Mock<TimeProvider>();
            _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(DateTimeOffset.UtcNow);

            var availabilityService = new AvailabilityService();
            var scoringService = new ScoringService();
            var userDetailsService = new UserDetailsService(_context);
            var userTricklesService = new UserTricklesService(_context, scoringService, _timeProviderMock.Object, userDetailsService);
            var answersService = new AnswersService(_context, availabilityService, null, _timeProviderMock.Object, scoringService, userDetailsService, userTricklesService);

            _service = new TricklerService(_context, _loggerMock.Object, _timeProviderMock.Object, availabilityService, answersService);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }


        [Fact]
        public async Task GetTrickleByIdAsync_ExistingId_ShouldReturnTrickle()
        {
            var trickle = new Trickle { Title = "Test title", Text = "Test trickle" };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var result = await _service.GetTrickleByIdAsync(trickle.Id);

            Assert.NotNull(result);
            Assert.Equal(trickle.Id, result.Id);
            Assert.Equal(trickle.Text, result.Text);
            Assert.Equal(trickle.Title, result.Title);
        }

        [Fact]
        public async Task GetTrickleByIdAsync_NonExistingId_ShouldReturnNull()
        {
            var nonExistingId = 999;

            var result = await _service.GetTrickleByIdAsync(nonExistingId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllTricklesAsync_ShouldReturnAllTrickles()
        {
            // create an availability and attach to one trickle, add answers to another
            var availability = new Availability
            {
                Type = AvailabilityType.Weekly,
                DaysOfWeek = ["monday", "friday"]
            };
            _context.Availabilities.Add(availability);
            await _context.SaveChangesAsync();

            var trickles = new[]
            {
                new Trickle { Title = "T1", Text = "Trickle 1", AvailabilityId = availability.Id },
                new Trickle { Title = "T2", Text = "Trickle 2" },
                new Trickle { Title = "T3", Text = "Trickle 3" }
            };
            _context.Trickles.AddRange(trickles);
            await _context.SaveChangesAsync();

            _context.Answers.Add(new Answer { TricklerId = trickles[1].Id, AnswerText = "A1" });
            await _context.SaveChangesAsync();

            var result = await _service.GetAllTricklesAsync();

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, t => t.Text == "Trickle 1");
            Assert.Contains(result, t => t.Text == "Trickle 2");
            Assert.Contains(result, t => t.Text == "Trickle 3");
            Assert.Contains(result, t => t.Title == "T1");
            Assert.Contains(result, t => t.Title == "T2");
            Assert.Contains(result, t => t.Title == "T3");

            var r1 = result.First(r => r.Id == trickles[0].Id);
            Assert.NotNull(r1.Availability);
            Assert.Equal("Weekly", r1.Availability.Type);
#pragma warning disable CS8604 // Possible null reference argument. not possible because we are mocking it directly. good to be caught in a test
            Assert.Contains("monday", collection: r1.Availability.DaysOfWeek);
#pragma warning restore CS8604 

            var r2 = result.First(r => r.Id == trickles[1].Id);
            Assert.NotNull(r2.Answers);
            Assert.Contains(r2.Answers, a => a.Answer == "A1");
        }

        [Fact]
        public async Task GetAllTricklesAsync_EmptyDatabase_ShouldReturnEmptyList()
        {
            var result = await _service.GetAllTricklesAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateTrickleAsync_ExistingTrickle_ShouldUpdateAllFields()
        {
            var trickle = new Trickle { Title = "Original title", Text = "Original text" };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var newTitle = "Updated title";
            var newText = "Updated text";
            var newAnswers = new[] { new AnswerDto(0, "Yes"), new AnswerDto(0, "No") };
            var availability = new AvailabilityDto(
                "DateRange",
                new DateOnly(2026, 2, 10),
                new DateOnly(2026, 2, 20),
                null,
                null
            );

            var result = await _service.UpdateTrickleAsync(trickle.Id, newTitle, newText, newAnswers, availability);

            Assert.NotNull(result);
            Assert.Equal(newTitle, result.Title);
            Assert.Equal(newText, result.Text);
            Assert.NotNull(result.Availability);
            Assert.Equal("DateRange", result.Availability.Type);

            var updatedTrickle = await _context.Trickles
                .Include(t => t.Availability)
                .FirstAsync(t => t.Id == trickle.Id);

            Assert.Equal(newTitle, updatedTrickle.Title);
            Assert.Equal(newText, updatedTrickle.Text);
            Assert.NotNull(updatedTrickle.Availability);
            Assert.Equal(AvailabilityType.DateRange, updatedTrickle.Availability.Type);
            Assert.Equal(new DateOnly(2026, 2, 10), updatedTrickle.Availability.From);
            Assert.Equal(new DateOnly(2026, 2, 20), updatedTrickle.Availability.Until);

            var answersInDb = await _context.Answers.Where(a => a.TricklerId == trickle.Id).ToListAsync();
            Assert.Equal(2, answersInDb.Count);
            Assert.Contains(answersInDb, a => a.AnswerText == "Yes");
            Assert.Contains(answersInDb, a => a.AnswerText == "No");
        }

        [Fact]
        public async Task UpdateTrickleAsync_NonExistingTrickle_ShouldThrowTrickleNotFoundExceptionAndLogWarning()
        {
            var nonExistingId = 999;
            var newText = "Updated text";

            await Assert.ThrowsAsync<TrickleNotFoundException>(async () =>
            {
                await _service.UpdateTrickleAsync(nonExistingId, "NewTitle", newText, null, null);
            });

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteTrickleAsync_ExistingTrickle_ShouldDeleteAndReturnTrue()
        {
            var trickle = new Trickle { Title = "ToDelete", Text = "To be deleted" };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var result = await _service.DeleteTrickleAsync(trickle.Id);

            Assert.True(result);

            var deletedTrickle = await _context.Trickles.FindAsync(trickle.Id);
            Assert.Null(deletedTrickle);
        }

        [Fact]
        public async Task DeleteTrickleAsync_NonExistingTrickle_ShouldThrowTrickleNotFoundExceptionAndLogWarning()
        {
            var nonExistingId = 999;

            await Assert.ThrowsAsync<TrickleNotFoundException>(async () =>
            {
                await _service.DeleteTrickleAsync(nonExistingId);
            });

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateTrickleWithAnswersAsync_ShouldCreateTrickleAndAnswers()
        {
            var text = "Question with answers";
            var answers = new[] { new AnswerDto(0, "Yes"), new AnswerDto(0, "No"), new AnswerDto(0, "Maybe") };

            var result = await _service.CreateTrickleAsync("Question title", text, answers, null);

            Assert.NotNull(result);
            Assert.Equal(text, result.Text);
            Assert.Equal("Question title", result.Title);
            Assert.True(result.Id > 0);

            var answersInDb = await _context.Answers.Where(a => a.TricklerId == result.Id).ToListAsync();
            Assert.Equal(3, answersInDb.Count);
            Assert.Contains(answersInDb, a => a.AnswerText == "Yes");
            Assert.Contains(answersInDb, a => a.AnswerText == "No");
            Assert.Contains(answersInDb, a => a.AnswerText == "Maybe");
        }

        [Fact]
        public async Task CreateTrickleWithAnswersAsync_WithAvailability_ShouldCreateTrickleWithAvailability()
        {
            var text = "Question with answers and availability";
            var answers = new[] { new AnswerDto(0, "Yes"), new AnswerDto(0, "No") };
            var availability = new AvailabilityDto(
                "DateRange",
                new DateOnly(2026, 2, 10),
                new DateOnly(2026, 2, 20),
                null,
                null
            );

            var result = await _service.CreateTrickleAsync("Question title", text, answers, availability);

            Assert.NotNull(result);
            Assert.Equal(text, result.Text);
            Assert.NotNull(result.Availability);
            Assert.Equal("DateRange", result.Availability.Type);

            var trickleInDb = await _context.Trickles
                .Include(t => t.Availability)
                .FirstAsync(t => t.Id == result.Id);
            Assert.NotNull(trickleInDb.Availability);
            Assert.Equal(AvailabilityType.DateRange, trickleInDb.Availability.Type);
            Assert.Equal(new DateOnly(2026, 2, 10), trickleInDb.Availability.From);
            Assert.Equal(new DateOnly(2026, 2, 20), trickleInDb.Availability.Until);
        }

        [Fact]
        public async Task CreateTrickleWithAnswersAsync_WithSpecificDatesAvailability_ShouldStoreListOfDates()
        {
            var text = "Question with specific dates";
            var availability = new AvailabilityDto(
                "specificDates",
                null,
                null,
                [new(2026, 2, 15), new(2026, 2, 20)],
                null
            );

            var result = await _service.CreateTrickleAsync("Question", text, null, availability);

            var trickleInDb = await _context.Trickles
                .Include(t => t.Availability)
                .FirstAsync(t => t.Id == result.Id);
            Assert.NotNull(trickleInDb.Availability);
            Assert.Equal(AvailabilityType.SpecificDates, trickleInDb.Availability.Type);
            Assert.NotNull(trickleInDb.Availability.Dates);
            Assert.Equal(2, trickleInDb.Availability.Dates.Count);
            Assert.Contains(new DateOnly(2026, 2, 15), trickleInDb.Availability.Dates);
            Assert.Contains(new DateOnly(2026, 2, 20), trickleInDb.Availability.Dates);
        }

        [Fact]
        public async Task CreateTrickleWithAnswersAsync_WithWeeklyAvailability_ShouldStoreListOfDays()
        {
            var text = "Question with weekly availability";
            var availability = new AvailabilityDto(
                "Weekly",
                null,
                null,
                null,
                ["monday", "wednesday", "friday"]
            );

            var result = await _service.CreateTrickleAsync("Question", text, null, availability);

            var trickleInDb = await _context.Trickles
                .Include(t => t.Availability)
                .FirstAsync(t => t.Id == result.Id);
            Assert.NotNull(trickleInDb.Availability);
            Assert.Equal(AvailabilityType.Weekly, trickleInDb.Availability.Type);
            Assert.NotNull(trickleInDb.Availability.DaysOfWeek);
            Assert.Equal(3, trickleInDb.Availability.DaysOfWeek.Count);
            Assert.Contains("monday", trickleInDb.Availability.DaysOfWeek);
            Assert.Contains("wednesday", trickleInDb.Availability.DaysOfWeek);
            Assert.Contains("friday", trickleInDb.Availability.DaysOfWeek);
        }

        [Fact]
        public async Task UpdateTrickleAsync_WithAvailability_ShouldCreateAndAttachAvailability()
        {
            var trickle = new Trickle { Title = "Test", Text = "Test text" };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var availability = new AvailabilityDto(
                "DateRange",
                new DateOnly(2026, 2, 10),
                new DateOnly(2026, 2, 20),
                null,
                null
            );

            var result = await _service.UpdateTrickleAsync(trickle.Id, "Updated", "Updated text", null, availability);

            Assert.NotNull(result);
            Assert.NotNull(result.Availability);
            Assert.Equal("DateRange", result.Availability.Type);

            var updatedTrickle = await _context.Trickles
                .Include(t => t.Availability)
                .FirstAsync(t => t.Id == trickle.Id);
            Assert.NotNull(updatedTrickle.Availability);
            Assert.Equal(AvailabilityType.DateRange, updatedTrickle.Availability.Type);
            Assert.Equal(new DateOnly(2026, 2, 10), updatedTrickle.Availability.From);
            Assert.Equal(new DateOnly(2026, 2, 20), updatedTrickle.Availability.Until);
        }

        [Fact]
        public async Task UpdateTrickleAsync_WithExistingAvailability_ShouldUpdateAvailability()
        {
            var availability = new Availability
            {
                Type = AvailabilityType.DateRange,
                From = new DateOnly(2026, 1, 1),
                Until = new DateOnly(2026, 1, 31)
            };
            _context.Availabilities.Add(availability);
            await _context.SaveChangesAsync();

            var trickle = new Trickle { Title = "Test", Text = "Test text", AvailabilityId = availability.Id };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var newAvailability = new AvailabilityDto(
                "DateRange",
                new DateOnly(2026, 2, 10),
                new DateOnly(2026, 2, 20),
                null,
                null
            );

            await _service.UpdateTrickleAsync(trickle.Id, "Updated", "Updated text", null, newAvailability);

            var updatedTrickle = await _context.Trickles
                .Include(t => t.Availability)
                .FirstAsync(t => t.Id == trickle.Id);
            Assert.NotNull(updatedTrickle.Availability);
            Assert.Equal(new DateOnly(2026, 2, 10), updatedTrickle.Availability.From);
            Assert.Equal(new DateOnly(2026, 2, 20), updatedTrickle.Availability.Until);
        }

        [Fact]
        public async Task UpdateTrickleAsync_ShouldClearAnswers_WhenEmptyListProvided()
        {
            var trickle = new Trickle { Title = "Q", Text = "T" };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            _context.Answers.AddRange(new Answer { TricklerId = trickle.Id, AnswerText = "A1" });
            await _context.SaveChangesAsync();

            var newAnswers = Array.Empty<AnswerDto>();

            var result = await _service.UpdateTrickleAsync(trickle.Id, trickle.Title, trickle.Text, newAnswers, null);

            var answersInDb = await _context.Answers.Where(a => a.TricklerId == trickle.Id).ToListAsync();
            Assert.Empty(answersInDb);
        }

        [Fact]
        public async Task UpdateTrickleAsync_ShouldNotChangeAnswers_WhenAnswersNull()
        {
            var trickle = new Trickle { Title = "Q", Text = "T" };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            _context.Answers.AddRange(new Answer { TricklerId = trickle.Id, AnswerText = "A1" });
            await _context.SaveChangesAsync();

            var result = await _service.UpdateTrickleAsync(trickle.Id, trickle.Title, "Updated text", null, null);

            var answersInDb = await _context.Answers.Where(a => a.TricklerId == trickle.Id).ToListAsync();
            Assert.Single(answersInDb);
            Assert.Equal("A1", answersInDb[0].AnswerText);
        }

        [Fact]
        public async Task GetAvailableTricklesAsync_ReturnsAllTricklesWithNoAvailability()
        {
            var t1 = new Trickle { Title = "Q1", Text = "Q1 text" };
            var t2 = new Trickle { Title = "Q2", Text = "Q2 text" };
            _context.Trickles.AddRange(t1, t2);
            await _context.SaveChangesAsync();

            var result = await _service.GetAvailableTricklesAsync();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAvailableTricklesForUserAsync_ReturnsHydratedUserState()
        {
            var trickle = new Trickle { Title = "Question", Text = "Test", Score = 100, AttemptsPerTrickle = 5 };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            _context.UserTrickles.Add(new UserTrickle
            {
                UserId = "user-1",
                TrickleId = trickle.Id,
                AttemptsToday = 1,
                AttemptsDate = DateTime.UtcNow.Date,
                AttemptCountTotal = 1,
                CurrentScore = 90,
                IsSolved = false
            });
            await _context.SaveChangesAsync();

            var result = await _service.GetAvailableTricklesForUserAsync("user-1");

            Assert.Equal("user-1", result.UserId);
            var item = Assert.Single(result.Trickles);
            Assert.True(item.HasAttempted);
            Assert.Equal(90, item.CurrentScore);
            Assert.Equal(4, item.AttemptsLeft);
            Assert.False(item.IsSolved);
        }

        [Fact]
        public async Task GetAvailableTricklesAsync_FiltersDateRangeAvailability()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var tomorrow = today.AddDays(1);
            var yesterday = today.AddDays(-1);

            // Trickle within range
            var availabilityInRange = new Availability
            {
                Type = AvailabilityType.DateRange,
                From = yesterday,
                Until = tomorrow
            };
            _context.Availabilities.Add(availabilityInRange);
            await _context.SaveChangesAsync();

            // Trickle before range
            var availabilityBefore = new Availability
            {
                Type = AvailabilityType.DateRange,
                From = null,
                Until = yesterday
            };
            _context.Availabilities.Add(availabilityBefore);
            await _context.SaveChangesAsync();

            // Trickle after range
            var availabilityAfter = new Availability
            {
                Type = AvailabilityType.DateRange,
                From = tomorrow,
                Until = null
            };
            _context.Availabilities.Add(availabilityAfter);
            await _context.SaveChangesAsync();

            var t1 = new Trickle { Title = "Q1", Text = "In range", AvailabilityId = availabilityInRange.Id };
            var t2 = new Trickle { Title = "Q2", Text = "Before range", AvailabilityId = availabilityBefore.Id };
            var t3 = new Trickle { Title = "Q3", Text = "After range", AvailabilityId = availabilityAfter.Id };
            _context.Trickles.AddRange(t1, t2, t3);
            await _context.SaveChangesAsync();

            var result = await _service.GetAvailableTricklesAsync();

            Assert.Single(result);
            Assert.Equal("Q1", result[0].Title);
        }

        [Fact]
        public async Task GetAvailableTricklesAsync_FiltersSpecificDatesAvailability()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var tomorrow = today.AddDays(1);

            var availabilityWithToday = new Availability
            {
                Type = AvailabilityType.SpecificDates,
                Dates = [today, tomorrow]
            };
            _context.Availabilities.Add(availabilityWithToday);
            await _context.SaveChangesAsync();

            var availabilityWithoutToday = new Availability
            {
                Type = AvailabilityType.SpecificDates,
                Dates = [tomorrow.AddDays(1), tomorrow.AddDays(2)]
            };
            _context.Availabilities.Add(availabilityWithoutToday);
            await _context.SaveChangesAsync();

            var t1 = new Trickle { Title = "Q1", Text = "Has today", AvailabilityId = availabilityWithToday.Id };
            var t2 = new Trickle { Title = "Q2", Text = "No today", AvailabilityId = availabilityWithoutToday.Id };
            _context.Trickles.AddRange(t1, t2);
            await _context.SaveChangesAsync();

            var result = await _service.GetAvailableTricklesAsync();

            Assert.Single(result);
            Assert.Equal("Q1", result[0].Title);
        }

        [Fact]
        public async Task GetAvailableTricklesAsync_FiltersWeeklyAvailability()
        {
            var today = DateTime.UtcNow;
            var todayDayOfWeek = today.DayOfWeek.ToString();

            var availabilityTodayOnly = new Availability
            {
                Type = AvailabilityType.Weekly,
                DaysOfWeek = [todayDayOfWeek]
            };
            _context.Availabilities.Add(availabilityTodayOnly);
            await _context.SaveChangesAsync();

            var availabilityOtherDays = new Availability
            {
                Type = AvailabilityType.Weekly,
                DaysOfWeek = [today.AddDays(1).DayOfWeek.ToString()]
            };
            _context.Availabilities.Add(availabilityOtherDays);
            await _context.SaveChangesAsync();

            var t1 = new Trickle { Title = "Q1", Text = "Today", AvailabilityId = availabilityTodayOnly.Id };
            var t2 = new Trickle { Title = "Q2", Text = "Other days", AvailabilityId = availabilityOtherDays.Id };
            _context.Trickles.AddRange(t1, t2);
            await _context.SaveChangesAsync();

            var result = await _service.GetAvailableTricklesAsync();

            Assert.Single(result);
            Assert.Equal("Q1", result[0].Title);
        }

        [Fact]
        public async Task GetAvailableTricklesAsync_MixedAvailability()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var todayDayOfWeek = DateTime.UtcNow.DayOfWeek.ToString();

            // No availability
            var t1 = new Trickle { Title = "Q1", Text = "No availability" };

            // Date range available
            var dateRangeAvailability = new Availability
            {
                Type = AvailabilityType.DateRange,
                From = today.AddDays(-1),
                Until = today.AddDays(1)
            };
            _context.Availabilities.Add(dateRangeAvailability);
            await _context.SaveChangesAsync();
            var t2 = new Trickle { Title = "Q2", Text = "Date range", AvailabilityId = dateRangeAvailability.Id };

            // Specific date available
            var specificDateAvailability = new Availability
            {
                Type = AvailabilityType.SpecificDates,
                Dates = [today]
            };
            _context.Availabilities.Add(specificDateAvailability);
            await _context.SaveChangesAsync();
            var t3 = new Trickle { Title = "Q3", Text = "Specific date", AvailabilityId = specificDateAvailability.Id };

            // Weekly available
            var weeklyAvailability = new Availability
            {
                Type = AvailabilityType.Weekly,
                DaysOfWeek = [todayDayOfWeek]
            };
            _context.Availabilities.Add(weeklyAvailability);
            await _context.SaveChangesAsync();
            var t4 = new Trickle { Title = "Q4", Text = "Weekly", AvailabilityId = weeklyAvailability.Id };

            _context.Trickles.AddRange(t1, t2, t3, t4);
            await _context.SaveChangesAsync();

            var result = await _service.GetAvailableTricklesAsync();

            Assert.Equal(4, result.Count);
        }
    }
}
