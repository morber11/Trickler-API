using Microsoft.EntityFrameworkCore;
using Trickler_API.Data;
using Trickler_API.Exceptions;
using Trickler_API.Models;

namespace Trickler_API.Services
{
    public class AnswersService(TricklerDbContext context,
        AvailabilityService? availabilityService = null,
        IConfiguration? configuration = null)
    {
        private readonly TricklerDbContext _context = context;
        private readonly AvailabilityService _availabilityService = availabilityService ?? new AvailabilityService();
        private readonly IConfiguration? _configuration = configuration;

        public record SubmitAnswerResult(bool Correct, string? RewardCode, int AttemptsLeft);

        public async Task<bool> VerifyAnswerAsync(int trickleId, string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return false;

            var trickleExists = await _context.Trickles.AnyAsync(t => t.Id == trickleId);
            if (!trickleExists) return false;

            var normalizedAnswer = answer.Trim().ToLower();

#pragma warning disable CA1862 
            // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons.
            // EF won't treat this the same with StringComparison
            // also the EF.ILike function doesn't seem to work either 
            var match = await _context.Answers
                .Where(a => a.TricklerId == trickleId)
                .AnyAsync(a => a.AnswerText != null
                && a.AnswerText.ToLower() == normalizedAnswer);
#pragma warning restore CA1862

            return match;
        }

        public async Task<SubmitAnswerResult> SubmitAnswerAsync(int trickleId, string answer, string userId)
        {
            if (string.IsNullOrWhiteSpace(answer)) return new SubmitAnswerResult(false, null, 0);

            var trickle = await _context.Trickles.Include(t => t.Availability)
                .FirstOrDefaultAsync(t => t.Id == trickleId) 
                ?? throw new TrickleNotFoundException(trickleId);

            // maybe use timeprovider ???
            var today = DateTime.UtcNow.Date;
            var currentDateOnly = DateOnly.FromDateTime(DateTime.UtcNow);
            var currentDayOfWeek = DateTime.UtcNow.DayOfWeek.ToString();

            if (!_availabilityService.IsAvailable(trickle.Availability, currentDateOnly, currentDayOfWeek))
            {
                return new SubmitAnswerResult(false, null, 0);
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

            if (userTrickle.AttemptsToday >= attemptLimit)
            {
                // don't need to force this to be null because
                // the reward is generated later
                return new SubmitAnswerResult(false, userTrickle.RewardCode, 0);
            }

            userTrickle.AttemptsToday++;
            userTrickle.AttemptCountTotal++;
            userTrickle.LastAttemptAt = DateTime.UtcNow;

            var isCorrect = await VerifyAnswerAsync(trickleId, answer);

            if (isCorrect && !userTrickle.IsSolved)
            {
                userTrickle.IsSolved = true;
                userTrickle.SolvedAt = DateTime.UtcNow;
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
                        userTrickle.RowVersion = reloaded.RowVersion;
                        userTrickle.IsSolved = userTrickle.IsSolved || reloaded.IsSolved;
                        userTrickle.RewardCode ??= reloaded.RewardCode;
                    }
                }
            }

            var attemptsLeft = Math.Max(0, attemptLimit - userTrickle.AttemptsToday);
            return new SubmitAnswerResult(isCorrect, userTrickle.RewardCode, attemptsLeft);
        }
    }
}
