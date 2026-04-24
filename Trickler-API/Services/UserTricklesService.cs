using Microsoft.EntityFrameworkCore;
using Trickler_API.Data;
using Trickler_API.Models;

namespace Trickler_API.Services
{
    public class UserTricklesService(TricklerDbContext context,
        ScoringService scoringService,
        TimeProvider timeProvider,
        UserDetailsService userDetailsService)
    {
        private readonly TricklerDbContext _context = context;
        private readonly ScoringService _scoringService = scoringService;
        private readonly TimeProvider _timeProvider = timeProvider;
        private readonly UserDetailsService _userDetailsService = userDetailsService;

        public async Task<UserTrickle> GetOrCreateUserTrickleAsync(string userId, Trickle trickle, DateTime today)
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

        public async Task SaveWithConcurrencyRetryAsync(UserTrickle userTrickle, string userId, int trickleBaseScore, bool changedCurrentScore, int scoreToAdd)
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
                            var scoreToReduce = reloaded.CurrentScore == 0 ? trickleBaseScore : reloaded.CurrentScore;
                            userTrickle.CurrentScore = _scoringService.ApplyWrongAttempt(scoreToReduce, trickleBaseScore);
                        }
                    }

                    if (scoreToAdd > 0)
                    {
                        await _userDetailsService.MergeUserScoreFromDatabaseAsync(userId, scoreToAdd);
                    }
                }
            }
        }

        public async Task<int> CountSolvedAsync(string userId)
            => await _context.UserTrickles.CountAsync(ut => ut.UserId == userId && ut.IsSolved);

        public async Task<List<UserTrickle>> GetRecentSolvedAsync(string userId, int take = 5)
            => await _context.UserTrickles.Where(ut => ut.UserId == userId && ut.IsSolved).OrderByDescending(ut => ut.SolvedAt).Take(take).ToListAsync();

        public async Task<List<UserTrickle>> GetSolvedByUserAsync(string userId, int take = 30)
            => await _context.UserTrickles
                .Where(ut => ut.UserId == userId && ut.IsSolved)
                .Include(ut => ut.Trickle)
                .OrderByDescending(ut => ut.SolvedAt)
                .Take(take)
                .ToListAsync();

        public async Task<bool> HasUserSolvedTrickleAsync(int trickleId, string userId)
            => await _context.UserTrickles.AnyAsync(ut => ut.UserId == userId && ut.TrickleId == trickleId && ut.IsSolved);
    }
}
