using Microsoft.EntityFrameworkCore;
using Trickler_API.Data;
using Trickler_API.Models;
using Trickler_API.Services;

namespace Trickler_API.Tests.Services
{
    public class UserTricklesServiceTests : IDisposable
    {
        private readonly TricklerDbContext _context;
        private readonly UserTricklesService _service;
        private readonly UserDetailsService _userDetailsService;
        private readonly ScoringService _scoringService;

        public UserTricklesServiceTests()
        {
            var options = new DbContextOptionsBuilder<TricklerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TricklerDbContext(options);
            _scoringService = new ScoringService();
            _userDetailsService = new UserDetailsService(_context);
            _service = new UserTricklesService(_context, _scoringService, TimeProvider.System, _userDetailsService);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task GetOrCreateUserTrickle_CreatesAndTracks()
        {
            var trickle = new Trickle { Text = "Question", Score = 10 };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var today = DateTime.UtcNow.Date;
            var ut = await _service.GetOrCreateUserTrickleAsync("user-1", trickle, today);

            Assert.NotNull(ut);
            Assert.Equal("user-1", ut.UserId);
            Assert.Equal(trickle.Id, ut.TrickleId);
            Assert.Equal(trickle.Score, ut.CurrentScore);
            Assert.False(ut.IsSolved);
            Assert.Contains(ut, _context.UserTrickles.Local);
        }

        [Fact]
        public async Task GetOrCreateUserTrickle_ReturnsExisting()
        {
            var trickle = new Trickle { Text = "Question", Score = 5 };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var existing = new UserTrickle { UserId = "userX", TrickleId = trickle.Id, CurrentScore = 5, AttemptsDate = DateTime.UtcNow.Date };
            _context.UserTrickles.Add(existing);
            await _context.SaveChangesAsync();

            var ut = await _service.GetOrCreateUserTrickleAsync("userX", trickle, DateTime.UtcNow.Date);

            Assert.Equal(existing.Id, ut.Id);
            Assert.Equal("userX", ut.UserId);
        }

        [Fact]
        public async Task SaveWithConcurrencyRetryAsync_PersistsChanges()
        {
            var trickle = new Trickle { Text = "Question", Score = 7 };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var ut = await _service.GetOrCreateUserTrickleAsync("userY", trickle, DateTime.UtcNow.Date);
            ut.AttemptsToday++;
            await _service.SaveWithConcurrencyRetryAsync(ut, "userY", trickle.Score, changedCurrentScore: false, scoreToAdd: 0);

            var persisted = await _context.UserTrickles.SingleAsync(u => u.UserId == "userY" && u.TrickleId == trickle.Id);
            Assert.Equal(1, persisted.AttemptsToday);
        }

        [Fact]
        public async Task CountAndRecentSolved_Works()
        {
            var trickle = new Trickle { Text = "Question", Score = 3 };
            _context.Trickles.Add(trickle);
            await _context.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var t1 = new UserTrickle { UserId = "u", TrickleId = trickle.Id, IsSolved = true, SolvedAt = now.AddMinutes(-10) };
            var t2 = new UserTrickle { UserId = "u", TrickleId = trickle.Id, IsSolved = true, SolvedAt = now.AddMinutes(-5) };
            var t3 = new UserTrickle { UserId = "u", TrickleId = trickle.Id, IsSolved = false };

            _context.UserTrickles.AddRange(t1, t2, t3);
            await _context.SaveChangesAsync();

            var count = await _service.CountSolvedAsync("u");
            Assert.Equal(2, count);

            var recent = await _service.GetRecentSolvedAsync("u", 2);
            Assert.Equal(2, recent.Count);
            Assert.Equal(t2.SolvedAt, recent[0].SolvedAt);
            Assert.Equal(t1.SolvedAt, recent[1].SolvedAt);
        }

        [Fact]
        public async Task GetSolvedByUserAsync_ReturnsOnlySolvedForGivenUser()
        {
            var t1 = new Trickle { Text = "T1", Score = 1 };
            var t2 = new Trickle { Text = "T2", Score = 2 };
            _context.Trickles.AddRange(t1, t2);
            await _context.SaveChangesAsync();

            var ut1 = new UserTrickle { UserId = "userA", TrickleId = t1.Id, IsSolved = true, SolvedAt = DateTime.UtcNow.AddMinutes(-5) };
            var ut2 = new UserTrickle { UserId = "userA", TrickleId = t2.Id, IsSolved = false };
            var ut3 = new UserTrickle { UserId = "userB", TrickleId = t1.Id, IsSolved = true };

            _context.UserTrickles.AddRange(ut1, ut2, ut3);
            await _context.SaveChangesAsync();

            var results = await _service.GetSolvedByUserAsync("userA");
            Assert.Single(results);
            Assert.Equal(t1.Id, results[0].TrickleId);
            Assert.True(results[0].IsSolved);
        }

        [Fact]
        public async Task HasUserSolvedTrickle_ReturnsTrueWhenSolvedAndFalseWhenNot()
        {
            var t1 = new Trickle { Text = "Q1", Score = 1 };
            var t2 = new Trickle { Text = "Q2", Score = 1 };
            _context.Trickles.AddRange(t1, t2);
            await _context.SaveChangesAsync();

            var ut = new UserTrickle { UserId = "uY", TrickleId = t1.Id, IsSolved = true };
            _context.UserTrickles.Add(ut);
            await _context.SaveChangesAsync();

            Assert.True(await _service.HasUserSolvedTrickleAsync(t1.Id, "uY"));
            Assert.False(await _service.HasUserSolvedTrickleAsync(t2.Id, "uY"));
        }
    }
}
