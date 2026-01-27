using Trickler_API.Models;

namespace Trickler_API.Services
{
    public class AvailabilityService
    {
        public bool IsAvailable(Availability? availability, DateOnly currentDate, string currentDayOfWeek)
        {
            if (availability is null)
            {
                return true;
            }

            return availability.Type switch
            {
                AvailabilityType.DateRange =>
                    (availability.From is null || currentDate >= availability.From) &&
                    (availability.Until is null || currentDate <= availability.Until),

                AvailabilityType.SpecificDates =>
                    availability.Dates is not null && availability.Dates.Contains(currentDate),

                AvailabilityType.Weekly =>
                    availability.DaysOfWeek is not null && availability.DaysOfWeek.Any(d => string.Equals(d?.Trim(), currentDayOfWeek, StringComparison.OrdinalIgnoreCase)),

                _ => false
            };
        }
    }
}