using Microsoft.EntityFrameworkCore;
using Trickler_API.Constants;
using Trickler_API.Data;
using Trickler_API.DTO;
using Trickler_API.Exceptions;
using Trickler_API.Models;

namespace Trickler_API.Services
{
    public class TricklerService(TricklerDbContext context,
        ILogger<TricklerService> logger,
        TimeProvider timeProvider,
        AvailabilityService availabilityService,
        AnswersService answersService)
    {
        private readonly TricklerDbContext _context = context;
        private readonly ILogger<TricklerService> _logger = logger;
        private readonly TimeProvider _timeProvider = timeProvider; // better for mocking instead of using DateTime.UTCNow directly
        private readonly AvailabilityService _availabilityService = availabilityService;
        private readonly AnswersService _answersService = answersService;

        public async Task<UserTrickleProgressDto> GetAvailableTrickleForUserAsync(string userId)
        {
            _logger.LogInformation("Getting available trickle for user {UserId}", userId);

            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
            var currentDate = DateOnly.FromDateTime(utcNow);
            var currentDayOfWeek = utcNow.DayOfWeek.ToString();

            var availableTrickles = await GetAvailableTrickleEntitiesAsync(currentDate, currentDayOfWeek);
            if (availableTrickles.Count == 0)
            {
                return new UserTrickleProgressDto(userId, null);
            }

            var dailyTrickle = SelectDailyTrickle(availableTrickles, currentDate);
            var userTrickle = await _context.UserTrickles
                .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TrickleId == dailyTrickle.Id);

            return new UserTrickleProgressDto(userId, HydrateTrickle(dailyTrickle, userTrickle, currentDate));
        }

        public async Task<TrickleWithAnswersDto> CreateTrickleAsync(
            string title,
            string text,
            IEnumerable<AnswerDto>? answers,
            AvailabilityDto? availability,
            int score = 0,
            string? rewardText = null,
            int attemptsPerTrickle = -1)
        {
            _logger.LogInformation("Creating new trickle with answers");

            var trickle = new Trickle { Title = title, Text = text, Score = score, RewardText = rewardText ?? string.Empty, AttemptsPerTrickle = attemptsPerTrickle };

            if (availability is not null)
            {
                trickle.Availability = MapDtoToAvailability(availability);
            }

            if (answers is not null)
            {
                var answerEntities = answers
                    .Where(a => a is not null && !string.IsNullOrWhiteSpace(a.Answer))
                    .Select(a => new Answer { AnswerText = a.Answer.Trim(), NormalizedAnswer = a.Answer.Trim().ToLowerInvariant() })
                    .ToList();

                if (answerEntities.Count != 0)
                {
                    trickle.Answers = answerEntities;
                }
            }

            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var dto = new TrickleWithAnswersDto(
                trickle.Id,
                trickle.Title,
                trickle.Text,
                trickle.Answers?.Select(a => new AnswerDto(a.Id, a.AnswerText)) ?? [],
                trickle.Score,
                trickle.RewardText,
                trickle.Availability is not null ? MapAvailabilityToDto(trickle.Availability) : null,
                trickle.AttemptsPerTrickle
            );

            return dto;
        }

        public async Task<TrickleWithAnswersDto?> GetTrickleByIdAsync(int id)
        {
            _logger.LogInformation("Getting trickle with ID: {Id}", id);

            var trickle = await _context.Trickles
                .Include(t => t.Answers)
                .Include(t => t.Availability)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trickle is null)
            {
                return null;
            }

            return new TrickleWithAnswersDto(
                trickle.Id,
                trickle.Title,
                trickle.Text,
                trickle.Answers.Select(a => new AnswerDto(a.Id, a.AnswerText)),
                trickle.Score,
                trickle.RewardText,
                trickle.Availability is not null ? MapAvailabilityToDto(trickle.Availability) : null,
                trickle.AttemptsPerTrickle
            );
        }

        public async Task<List<TrickleWithAnswersDto>> GetAllTricklesAsync()
        {
            _logger.LogInformation("Getting all trickles with answers");

            var trickles = await _context.Trickles
                .Include(t => t.Answers)
                .Include(t => t.Availability)
                .ToListAsync();

            return [.. trickles.Select(t => new TrickleWithAnswersDto(
                t.Id,
                t.Title,
                t.Text,
                t.Answers.Select(a => new AnswerDto(a.Id, a.AnswerText)),
                t.Score,
                t.RewardText,
                t.Availability is not null ? MapAvailabilityToDto(t.Availability) : null,
                t.AttemptsPerTrickle
            ))];
        }

        public async Task<TrickleWithAnswersDto> UpdateTrickleAsync(
            int id,
            string title,
            string text,
            IEnumerable<AnswerDto>? answers,
            AvailabilityDto? availability,
            int score = 0,
            string? rewardText = null,
            int attemptsPerTrickle = -1)
        {
            _logger.LogInformation("Updating trickle with ID: {Id}", id);

            var trickle = await _context.Trickles
                .Include(t => t.Availability)
                .Include(t => t.Answers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trickle is null)
            {
                _logger.LogWarning("Trickle with ID {Id} not found", id);
                throw new TrickleNotFoundException(id);
            }

            trickle.Title = title;
            trickle.Text = text;
            trickle.Score = score;
            trickle.RewardText = rewardText ?? string.Empty;
            trickle.AttemptsPerTrickle = attemptsPerTrickle;

            if (availability is not null)
            {
                if (trickle.Availability is null)
                {
                    var availabilityEntity = MapDtoToAvailability(availability);
                    trickle.Availability = availabilityEntity;
                }
                else
                {
                    UpdateAvailabilityEntity(trickle.Availability, availability);
                }
            }
            else
            {
                trickle.AvailabilityId = null;
            }

            if (answers is not null)
            {
                await _answersService.RemoveAnswersForTrickleAsync(trickle);

                var answerEntities = answers
                    .Where(a => a is not null && !string.IsNullOrWhiteSpace(a.Answer))
                    .Select(a => new Answer { TricklerId = trickle.Id, AnswerText = a.Answer.Trim(), NormalizedAnswer = a.Answer.Trim().ToLowerInvariant() })
                    .ToList();

                trickle.Answers = answerEntities.Count != 0 ? answerEntities : [];
            }

            await _context.SaveChangesAsync();

            return new TrickleWithAnswersDto(
                trickle.Id,
                trickle.Title,
                trickle.Text,
                trickle.Answers?.Select(a => new AnswerDto(a.Id, a.AnswerText)) ?? [],
                trickle.Score,
                trickle.RewardText,
                trickle.Availability is not null ? MapAvailabilityToDto(trickle.Availability) : null,
                trickle.AttemptsPerTrickle
            );
        }

        public async Task<bool> DeleteTrickleAsync(int id)
        {
            _logger.LogInformation("Deleting trickle with ID: {Id}", id);

            var trickle = await _context.Trickles
                .Include(t => t.Answers)
                .Include(t => t.Availability)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trickle is null)
            {
                _logger.LogWarning("Trickle with ID {Id} not found", id);
                throw new TrickleNotFoundException(id);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _answersService.RemoveAnswersForTrickleAsync(trickle);

                _context.Trickles.Remove(trickle);

                if (trickle.Availability is not null)
                {
                    _context.Availabilities.Remove(trickle.Availability);
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Concurrency conflict occurred while deleting trickle with ID {Id}", id);
                throw;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<List<Trickle>> GetAvailableTrickleEntitiesAsync(DateOnly currentDate, string currentDayOfWeek)
        {
            var allTrickles = await _context.Trickles
                .Include(t => t.Availability)
                .ToListAsync();

            return [.. allTrickles.Where(t => _availabilityService.IsAvailable(t.Availability, currentDate, currentDayOfWeek))];
        }

        private HydratedTrickleDto HydrateTrickle(Trickle trickle, UserTrickle? userTrickle, DateOnly currentDate)
        {
            var (isUnlimited, attemptLimit) = _answersService.GetAttemptLimit(trickle);
            var attemptsToday = userTrickle is not null && DateOnly.FromDateTime(userTrickle.AttemptsDate.Date) == currentDate
                ? userTrickle.AttemptsToday
                : 0;
            var attemptsLeft = isUnlimited
                ? int.MaxValue
                : Math.Max(0, attemptLimit - attemptsToday);

            return new HydratedTrickleDto(
                trickle.Id,
                trickle.Title,
                trickle.Text,
                trickle.Score,
                trickle.RewardText,
                trickle.Availability is not null ? MapAvailabilityToDto(trickle.Availability) : null,
                trickle.AttemptsPerTrickle,
                attemptsLeft,
                userTrickle?.IsSolved ?? false,
                userTrickle?.CurrentScore ?? trickle.Score
            );
        }

        private static Trickle SelectDailyTrickle(List<Trickle> availableTrickles, DateOnly currentDate)
        {
            var orderedTrickles = availableTrickles
                .OrderBy(trickle => trickle.Id)
                .ToList();

            return orderedTrickles[currentDate.DayNumber % orderedTrickles.Count];
        }

        // util functions for mapping only
        private static Availability MapDtoToAvailability(AvailabilityDto dto)
        {
            return new Availability
            {
                Type = Enum.TryParse<AvailabilityType>(dto.Type, true, out var parsedType)
                    ? parsedType
                    : throw new AppValidationException(string.Format(MessageConstants.Validation.InvalidAvailabilityTypeFormat, dto.Type)),
                From = dto.From,
                Until = dto.Until,
                Dates = dto.Dates,
                DaysOfWeek = dto.DaysOfWeek
            };
        }

        private static AvailabilityDto MapAvailabilityToDto(Availability availability)
        {
            return new AvailabilityDto(
                availability.Type.ToString(),
                availability.From,
                availability.Until,
                availability.Dates,
                availability.DaysOfWeek
            );
        }
        private static void UpdateAvailabilityEntity(Availability entity, AvailabilityDto dto)
        {
            entity.Type = Enum.TryParse<AvailabilityType>(dto.Type, true, out var parsed)
                ? parsed
                : throw new AppValidationException(string.Format(MessageConstants.Validation.InvalidAvailabilityTypeFormat, dto.Type));
            entity.From = dto.From;
            entity.Until = dto.Until;
            entity.Dates = dto.Dates;
            entity.DaysOfWeek = dto.DaysOfWeek;
        }

    }
}
