using Trickler_API.Models;
using Trickler_API.Services;

namespace Trickler_API.Tests.Services
{
    public class AvailabilityServiceTests
    {
        private readonly AvailabilityService _service = new();

        [Fact]
        public void IsAvailable_NullAvailability_ReturnsTrue()
        {
            var currentDate = new DateOnly(2026, 2, 9);
            var currentDayOfWeek = "Sunday";

            var result = _service.IsAvailable(null, currentDate, currentDayOfWeek);

            Assert.True(result);
        }

        [Theory]
        [InlineData(null, null, "2026-02-09", true)] // No date constraints
        [InlineData("2026-02-01", null, "2026-02-09", true)] // Only From date, current is after
        [InlineData("2026-02-01", null, "2026-01-15", false)] // Only From date, current is before
        [InlineData(null, "2026-02-15", "2026-02-09", true)] // Only Until date, current is before
        [InlineData(null, "2026-02-15", "2026-02-20", false)] // Only Until date, current is after
        [InlineData("2026-02-01", "2026-02-15", "2026-02-09", true)] // Within range
        [InlineData("2026-02-01", "2026-02-15", "2026-01-15", false)] // Before range
        [InlineData("2026-02-01", "2026-02-15", "2026-02-20", false)] // After range
        public void IsAvailable_DateRange_VariousScenarios(string? fromDate, string? untilDate, string currentDateStr, bool expected)
        {
            var currentDate = DateOnly.Parse(currentDateStr);
            var currentDayOfWeek = "Sunday"; // Not used for date range

            var availability = new Availability
            {
                Type = AvailabilityType.DateRange,
                From = fromDate is not null ? DateOnly.Parse(fromDate) : null,
                Until = untilDate is not null ? DateOnly.Parse(untilDate) : null
            };

            var result = _service.IsAvailable(availability, currentDate, currentDayOfWeek);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsAvailable_SpecificDates_IncludesCurrentDate_ReturnsTrue()
        {
            var currentDate = new DateOnly(2026, 2, 9);
            var currentDayOfWeek = "Sunday";

            var availability = new Availability
            {
                Type = AvailabilityType.SpecificDates,
                Dates = [new(2026, 2, 9), new DateOnly(2026, 2, 10)]
            };

            var result = _service.IsAvailable(availability, currentDate, currentDayOfWeek);

            Assert.True(result);
        }

        [Fact]
        public void IsAvailable_SpecificDates_ExcludesCurrentDate_ReturnsFalse()
        {
            var currentDate = new DateOnly(2026, 2, 9);
            var currentDayOfWeek = "Sunday";

            var availability = new Availability
            {
                Type = AvailabilityType.SpecificDates,
                Dates = [new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 11)]
            };

            var result = _service.IsAvailable(availability, currentDate, currentDayOfWeek);

            Assert.False(result);
        }

        [Fact]
        public void IsAvailable_SpecificDates_NullDates_ReturnsFalse()
        {
            var currentDate = new DateOnly(2026, 2, 9);
            var currentDayOfWeek = "Sunday";

            var availability = new Availability
            {
                Type = AvailabilityType.SpecificDates,
                Dates = null
            };

            var result = _service.IsAvailable(availability, currentDate, currentDayOfWeek);

            Assert.False(result);
        }

        [Fact]
        public void IsAvailable_Weekly_IncludesCurrentDay_ReturnsTrue()
        {
            var currentDate = new DateOnly(2026, 2, 9); // Sunday
            var currentDayOfWeek = "Sunday";

            var availability = new Availability
            {
                Type = AvailabilityType.Weekly,
                DaysOfWeek = ["Sunday", "Monday"]
            };

            var result = _service.IsAvailable(availability, currentDate, currentDayOfWeek);

            Assert.True(result);
        }

        [Fact]
        public void IsAvailable_Weekly_ExcludesCurrentDay_ReturnsFalse()
        {
            var currentDate = new DateOnly(2026, 2, 9); // Sunday
            var currentDayOfWeek = "Sunday";

            var availability = new Availability
            {
                Type = AvailabilityType.Weekly,
                DaysOfWeek = ["Monday", "Tuesday"],
            };

            var result = _service.IsAvailable(availability, currentDate, currentDayOfWeek);

            Assert.False(result);
        }

        [Fact]
        public void IsAvailable_Weekly_CaseInsensitiveMatch_ReturnsTrue()
        {
            var currentDate = new DateOnly(2026, 2, 9); // Sunday
            var currentDayOfWeek = "sunday"; // lowercase

            var availability = new Availability
            {
                Type = AvailabilityType.Weekly,
                DaysOfWeek = ["SUNDAY"] // uppercase
            };

            var result = _service.IsAvailable(availability, currentDate, currentDayOfWeek);

            Assert.True(result);
        }

        [Fact]
        public void IsAvailable_Weekly_WithWhitespace_ReturnsTrue()
        {
            var currentDate = new DateOnly(2026, 2, 9); // Sunday
            var currentDayOfWeek = "Sunday";

            var availability = new Availability
            {
                Type = AvailabilityType.Weekly,
                DaysOfWeek = [" Sunday ", " Monday "] // with whitespace
            };

            var result = _service.IsAvailable(availability, currentDate, currentDayOfWeek);

            Assert.True(result);
        }

        [Fact]
        public void IsAvailable_Weekly_NullDaysOfWeek_ReturnsFalse()
        {
            var currentDate = new DateOnly(2026, 2, 9);
            var currentDayOfWeek = "Sunday";

            var availability = new Availability
            {
                Type = AvailabilityType.Weekly,
                DaysOfWeek = null
            };

            var result = _service.IsAvailable(availability, currentDate, currentDayOfWeek);

            Assert.False(result);
        }

        [Fact]
        public void IsAvailable_UnknownType_ReturnsFalse()
        {
            var currentDate = new DateOnly(2026, 2, 9);
            var currentDayOfWeek = "Sunday";

            var availability = new Availability
            {
                Type = (AvailabilityType)999 // Invalid type
            };

            var result = _service.IsAvailable(availability, currentDate, currentDayOfWeek);

            Assert.False(result);
        }
    }
}