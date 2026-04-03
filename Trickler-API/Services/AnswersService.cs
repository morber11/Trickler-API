using Microsoft.EntityFrameworkCore;
using Trickler_API.Data;
using Trickler_API.Exceptions;
using Trickler_API.Models;

namespace Trickler_API.Services
{
    public class AnswersService(TricklerDbContext context,
        AvailabilityService availabilityService,
        IConfiguration? configuration,
        TimeProvider timeProvider,
        ScoringService scoringService,
        UserDetailsService userDetailsService)
    {
        private readonly TricklerDbContext _context = context;
        private readonly AvailabilityService _availabilityService = availabilityService;
        private readonly IConfiguration? _configuration = configuration;
        private readonly TimeProvider _timeProvider = timeProvider;
        private readonly ScoringService _scoringService = scoringService;
        private readonly UserDetailsService _userDetailsService = userDetailsService;

        public record SubmitAnswerResult(bool IsSolved, DateTime? SolvedAt, string? RewardCode, int AttemptsLeft);

        public async Task<SubmitAnswerResult> SubmitAnswerAsync(int trickleId, string answer, string userId)
        {
            if (string.IsNullOrWhiteSpace(answer)) return new SubmitAnswerResult(false, null, null, 0);

            var trickle = await LoadTrickleAsync(trickleId);

            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
            var today = utcNow.Date;
            var currentDateOnly = DateOnly.FromDateTime(utcNow);
            var currentDayOfWeek = utcNow.DayOfWeek.ToString();

            if (!IsTrickleAvailable(trickle, currentDateOnly, currentDayOfWeek))
            {
                return new SubmitAnswerResult(false, null, null, 0);
            }

            var (isUnlimited, attemptLimit) = ResolveAttemptLimit(trickle);

            var userTrickle = await GetOrCreateUserTrickleAsync(userId, trickle, today);

            if (userTrickle.AttemptsDate.Date != today)
            {
                userTrickle.AttemptsToday = 0;
                userTrickle.AttemptsDate = today;
            }

            if (TryGetShortCircuitResultIfSolved(userTrickle, isUnlimited, attemptLimit, out var earlyResult))
            {
                return earlyResult;
            }

            userTrickle.AttemptsToday++;
            userTrickle.AttemptCountTotal++;
            userTrickle.LastAttemptAt = _timeProvider.GetUtcNow().UtcDateTime;

            var normalizedAnswer = NormalizeAnswer(answer);
            var isCorrect = await _context.Answers
                .Where(a => a.TricklerId == trickleId)
                .AnyAsync(a => a.NormalizedAnswer == normalizedAnswer);

            var changedCurrentScore = false;
            var scoreToAdd = 0;
            if (isCorrect && !userTrickle.IsSolved)
            {
                scoreToAdd = userTrickle.CurrentScore == 0 ? trickle.Score : userTrickle.CurrentScore;
                userTrickle.IsSolved = true;
                userTrickle.SolvedAt = _timeProvider.GetUtcNow().UtcDateTime;
                userTrickle.RewardCode = GenerateRewardCode();
            }
            else if (!isCorrect)
            {
                userTrickle.CurrentScore = _scoringService.ApplyWrongAttempt(userTrickle.CurrentScore);
                changedCurrentScore = true;
            }

            if (scoreToAdd > 0)
            {
                await _userDetailsService.UpdateUserScoreAsync(userId, scoreToAdd);
            }

            await SaveWithConcurrencyRetryAsync(userTrickle, userId, trickle.Score, changedCurrentScore, scoreToAdd);

            var attemptsLeft = ComputeAttemptsLeft(isUnlimited, attemptLimit, userTrickle);
            return new SubmitAnswerResult(
                userTrickle.IsSolved,
                userTrickle.SolvedAt,
                userTrickle.RewardCode,
                attemptsLeft);
        }

        private static string NormalizeAnswer(string answer) => answer.Trim().ToLowerInvariant();

        private async Task<Trickle> LoadTrickleAsync(int trickleId)
        {
            return await _context.Trickles.Include(t => t.Availability)
                .FirstOrDefaultAsync(t => t.Id == trickleId)
                ?? throw new TrickleNotFoundException(trickleId);
        }

        private bool IsTrickleAvailable(Trickle trickle, DateOnly date, string dayOfWeek)
            => _availabilityService.IsAvailable(trickle.Availability, date, dayOfWeek);

        private (bool isUnlimited, int attemptLimit) ResolveAttemptLimit(Trickle trickle)
        {
            var isUnlimited = false;
            var attemptLimit = Constants.DefaultValueConstants.DefaultSubmitAnswerAttempts;

            var attemptsPerTrickle = trickle.AttemptsPerTrickle;
            if (attemptsPerTrickle == -1)
            {
                isUnlimited = true;
            }
            else if (attemptsPerTrickle > 0)
            {
                attemptLimit = attemptsPerTrickle;
            }
            else
            {
                if (_configuration is not null)
                {
                    var configured = _configuration["AttemptLimitPerDay"];
                    if (!string.IsNullOrWhiteSpace(configured) && int.TryParse(configured, out var parsed))
                    {
                        attemptLimit = parsed;
                    }
                }
            }

            return (isUnlimited, attemptLimit);
        }

        private async Task<UserTrickle> GetOrCreateUserTrickleAsync(string userId, Trickle trickle, DateTime today)
        {
            var userTrickle = await _context.UserTrickles.FirstOrDefaultAsync(u => u.UserId == userId && u.TrickleId == trickle.Id);
            if (userTrickle is null)
            {
                userTrickle = new UserTrickle
                {
                    UserId = userId,
                    TrickleId = trickle.Id,
                    AttemptsToday = 0,
                    AttemptsDate = today,
                    AttemptCountTotal = 0,
                    IsSolved = false,
                    CurrentScore = trickle.Score
                };
                _context.UserTrickles.Add(userTrickle);
            }

            return userTrickle;
        }

        private static bool TryGetShortCircuitResultIfSolved(UserTrickle userTrickle, bool isUnlimited, int attemptLimit, out SubmitAnswerResult result)
        {
            if (userTrickle.IsSolved)
            {
                var attemptsLeftCurrent = isUnlimited ? int.MaxValue : Math.Max(0, attemptLimit - userTrickle.AttemptsToday);
                result = new SubmitAnswerResult(
                    userTrickle.IsSolved,
                    userTrickle.SolvedAt,
                    userTrickle.RewardCode,
                    attemptsLeftCurrent);
                return true;
            }

            if (!isUnlimited && userTrickle.AttemptsToday >= attemptLimit)
            {
                result = new SubmitAnswerResult(false, null, userTrickle.RewardCode, 0);
                return true;
            }

            result = default!;
            return false;
        }

        private static string GenerateRewardCode() => Guid.NewGuid().ToString("N");

        private static int ComputeAttemptsLeft(bool isUnlimited, int attemptLimit, UserTrickle userTrickle)
            => isUnlimited ? int.MaxValue : Math.Max(0, attemptLimit - userTrickle.AttemptsToday);

        private async Task SaveWithConcurrencyRetryAsync(UserTrickle userTrickle, string userId, int trickleBaseScore, bool changedCurrentScore, int scoreToAdd)
        {
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
                    var reloaded = await _context.UserTrickles.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId
                    && u.TrickleId == userTrickle.TrickleId);

                    if (reloaded is not null)
                    {
                        userTrickle.IsSolved = userTrickle.IsSolved || reloaded.IsSolved;
                        userTrickle.RewardCode ??= reloaded.RewardCode;
                        if (changedCurrentScore)
                        {
                            var baseScore = reloaded.CurrentScore == 0 ? trickleBaseScore : reloaded.CurrentScore;
                            userTrickle.CurrentScore = _scoringService.ApplyWrongAttempt(baseScore);
                        }
                    }

                    if (scoreToAdd > 0)
                    {
                        await _userDetailsService.MergeUserScoreFromDatabaseAsync(userId, scoreToAdd);
                    }
                }
            }
        }
    }
}
