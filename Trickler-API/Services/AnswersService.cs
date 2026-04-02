using Microsoft.EntityFrameworkCore;
using Trickler_API.Data;
using Trickler_API.Exceptions;
using Trickler_API.Models;

namespace Trickler_API.Services
{
    public class AnswersService(TricklerDbContext context,
        AvailabilityService availabilityService,
        IConfiguration? configuration,
        TimeProvider timeProvider)
    {
        private readonly TricklerDbContext _context = context;
        private readonly AvailabilityService _availabilityService = availabilityService;
        private readonly IConfiguration? _configuration = configuration;
        private readonly TimeProvider _timeProvider = timeProvider;

        public record SubmitAnswerResult(bool IsSolved, DateTime? SolvedAt, string? RewardCode, int AttemptsLeft);

        public async Task<SubmitAnswerResult> SubmitAnswerAsync(int trickleId, string answer, string userId)
        {
            if (string.IsNullOrWhiteSpace(answer)) return new SubmitAnswerResult(false, null, null, 0);

            var trickle = await _context.Trickles.Include(t => t.Availability)
                .FirstOrDefaultAsync(t => t.Id == trickleId)
                ?? throw new TrickleNotFoundException(trickleId);

            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
            var today = utcNow.Date;
            var currentDateOnly = DateOnly.FromDateTime(utcNow);
            var currentDayOfWeek = utcNow.DayOfWeek.ToString();

            if (!_availabilityService.IsAvailable(trickle.Availability, currentDateOnly, currentDayOfWeek))
            {
                return new SubmitAnswerResult(false, null, null, 0);
            }

            var attemptLimit = Constants.DefaultValueConstants.DefaultSubmitAnswerAttempts;
            if (_configuration is not null)
            {
                var configured = _configuration["AttemptLimitPerDay"];
                if (!string.IsNullOrWhiteSpace(configured) && int.TryParse(configured, out var parsed))
                {
                    attemptLimit = parsed;
                }
            }

            var userTrickle = await _context.UserTrickles.FirstOrDefaultAsync(u => u.UserId == userId && u.TrickleId == trickleId);
            if (userTrickle is null)
            {
                userTrickle = new UserTrickle
                {
                    UserId = userId,
                    TrickleId = trickleId,
                    AttemptsToday = 0,
                    AttemptsDate = today,
                    AttemptCountTotal = 0,
                    IsSolved = false
                };
                _context.UserTrickles.Add(userTrickle);
            }

            if (userTrickle.AttemptsDate.Date != today)
            {
                userTrickle.AttemptsToday = 0;
                userTrickle.AttemptsDate = today;
            }

            // If the user already solved this trickle, short-circuit and return the solved state
            if (userTrickle.IsSolved)
            {
                var attemptsLeftCurrent = Math.Max(0, attemptLimit - userTrickle.AttemptsToday);
                return new SubmitAnswerResult(
                    userTrickle.IsSolved,
                    userTrickle.SolvedAt,
                    userTrickle.RewardCode,
                    attemptsLeftCurrent);
            }

            if (userTrickle.AttemptsToday >= attemptLimit)
            {
                // don't need to force this to be null because
                // the reward is generated later
                return new SubmitAnswerResult(false, null, userTrickle.RewardCode, 0);
            }

            userTrickle.AttemptsToday++;
            userTrickle.AttemptCountTotal++;
            userTrickle.LastAttemptAt = _timeProvider.GetUtcNow().UtcDateTime;

            var normalizedAnswer = answer.Trim().ToLowerInvariant();
            var isCorrect = await _context.Answers
                .Where(a => a.TricklerId == trickleId)
                .AnyAsync(a => a.NormalizedAnswer == normalizedAnswer);

            if (isCorrect && !userTrickle.IsSolved)
            {
                userTrickle.IsSolved = true;
                userTrickle.SolvedAt = _timeProvider.GetUtcNow().UtcDateTime;
                userTrickle.RewardCode = Guid.NewGuid().ToString("N");
            }

            var saveAttempts = 0;
            while (true)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateConcurrencyException)
                {
                    saveAttempts++;
                    if (saveAttempts >= Constants.DefaultValueConstants.DefaultDBUpdateRetryAmount) throw;

                    var reloaded = await _context.UserTrickles.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId && u.TrickleId == trickleId);
                    if (reloaded is not null)
                    {
                        userTrickle.IsSolved = userTrickle.IsSolved || reloaded.IsSolved;
                        userTrickle.RewardCode ??= reloaded.RewardCode;
                    }
                }
            }

            var attemptsLeft = Math.Max(0, attemptLimit - userTrickle.AttemptsToday);
            return new SubmitAnswerResult(
                userTrickle.IsSolved,
                userTrickle.SolvedAt,
                userTrickle.RewardCode,
                attemptsLeft);
        }
    }
}
