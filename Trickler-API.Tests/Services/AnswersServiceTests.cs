using Microsoft.EntityFrameworkCore;
using Trickler_API.Data;
using Trickler_API.Models;
using Trickler_API.Services;

namespace Trickler_API.Tests.Services
{
    public class AnswersServiceTests : IDisposable
    {
        private readonly TricklerDbContext _context;
        private readonly AnswersService _service;

        public AnswersServiceTests()
        {
            var options = new DbContextOptionsBuilder<TricklerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TricklerDbContext(options);
            _service = new AnswersService(_context, new AvailabilityService(), null, TimeProvider.System, new ScoringService());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task SubmitAnswer_VerifyInline_MatchingAnswer_ReturnsSolved()
        {
            var trickle = new Trickle { Text = "Question" };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var answer = new Answer { TricklerId = trickle.Id, AnswerText = "Yes" };
            _context.Answers.Add(answer);
            await _context.SaveChangesAsync();

            var userId = "verify-user";
            var result = await _service.SubmitAnswerAsync(trickle.Id, "Yes", userId);

            Assert.True(result.IsSolved);
        }

        [Fact]
        public async Task SubmitAnswer_Correct_GeneratesRewardAndMarksSolved()
        {
            var trickle = new Trickle { Text = "Question" };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            _context.Answers.Add(new Answer { TricklerId = trickle.Id, AnswerText = "yes" });
            await _context.SaveChangesAsync();

            var userId = "user-1";
            var result = await _service.SubmitAnswerAsync(trickle.Id, "yes", userId);

            Assert.True(result.IsSolved);
            Assert.False(string.IsNullOrWhiteSpace(result.RewardCode));

            var ut = await _context.UserTrickles.SingleAsync(u => u.UserId == userId && u.TrickleId == trickle.Id);
            Assert.True(ut.IsSolved);
            Assert.Equal(result.RewardCode, ut.RewardCode);
        }

        [Fact]
        public async Task SubmitAnswer_Incorrect_IncrementsAttemptCount()
        {
            var trickle = new Trickle { Text = "Question" };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var userId = "user-2";
            await _service.SubmitAnswerAsync(trickle.Id, "no", userId);
            await _service.SubmitAnswerAsync(trickle.Id, "no", userId);

            var ut = await _context.UserTrickles.SingleAsync(u => u.UserId == userId && u.TrickleId == trickle.Id);
            Assert.Equal(2, ut.AttemptsToday);
        }

        [Fact]
        public async Task SubmitAnswer_AttemptLimitEnforced()
        {
            var trickle = new Trickle { Text = "Question" };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var userId = "user-3";

            for (int i = 0; i < 5; i++)
            {
                var r = await _service.SubmitAnswerAsync(trickle.Id, "wrong", userId);
                Assert.False(r.IsSolved);
            }

            var sixth = await _service.SubmitAnswerAsync(trickle.Id, "wrong", userId);
            Assert.Equal(0, sixth.AttemptsLeft);
        }

        [Fact]
        public async Task SubmitAnswer_SecondCorrectReturnsSameReward()
        {
            var trickle = new Trickle { Text = "Question" };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            _context.Answers.Add(new Answer { TricklerId = trickle.Id, AnswerText = "yes" });
            await _context.SaveChangesAsync();

            var userId = "user-4";
            var first = await _service.SubmitAnswerAsync(trickle.Id, "yes", userId);
            var second = await _service.SubmitAnswerAsync(trickle.Id, "yes", userId);

            Assert.Equal(first.RewardCode, second.RewardCode);
            var ut = await _context.UserTrickles.SingleAsync(u => u.UserId == userId && u.TrickleId == trickle.Id);
            Assert.True(ut.IsSolved);
        }
    }
}