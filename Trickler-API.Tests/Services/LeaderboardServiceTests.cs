using Microsoft.EntityFrameworkCore;
using Trickler_API.Data;
using Trickler_API.Models;
using Trickler_API.Services;

namespace Trickler_API.Tests.Services
{
    public class LeaderboardServiceTests : IDisposable
    {
        private readonly TricklerDbContext _context;
        private readonly LeaderboardService _service;

        public LeaderboardServiceTests()
        {
            var options = new DbContextOptionsBuilder<TricklerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TricklerDbContext(options);
            _service = new LeaderboardService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task GetTopAsync_ReturnsPublicUsersSortedByTotalScore()
        {
            var u1 = new ApplicationUser { Id = "u1", UserName = "alice" };
            var u2 = new ApplicationUser { Id = "u2", UserName = "bob" };
            var u3 = new ApplicationUser { Id = "u3", UserName = "charlie" };
            var u4 = new ApplicationUser { Id = "u4", UserName = "dave" };

            _context.Users.AddRange(u1, u2, u3, u4);

            _context.Set<UserDetails>().AddRange(
                new UserDetails { UserId = "u1", TotalScore = 50, IsPrivate = false },
                new UserDetails { UserId = "u2", TotalScore = 100, IsPrivate = false },
                new UserDetails { UserId = "u3", TotalScore = 75, IsPrivate = true },
                new UserDetails { UserId = "u4", TotalScore = 25, IsPrivate = false }
            );

            await _context.SaveChangesAsync();

            var top = await _service.GetTopAsync(10);

            Assert.Equal(3, top.Count);
            Assert.Equal("bob", top[0].Username);
            Assert.Equal(100, top[0].TotalScore);
            Assert.Equal("alice", top[1].Username);
            Assert.Equal(50, top[1].TotalScore);
            Assert.Equal("dave", top[2].Username);
            Assert.Equal(25, top[2].TotalScore);
        }

        [Fact]
        public async Task GetTopAsync_RespectsTakeParameter()
        {
            var users = Enumerable.Range(1, 5).Select(i => new ApplicationUser { Id = $"u{i}", UserName = $"u{i}" }).ToList();
            _context.Users.AddRange(users);

            var details = users.Select((u, idx) => new UserDetails { UserId = u.Id, TotalScore = (5 - idx) * 10, IsPrivate = false }).ToList();
            _context.Set<UserDetails>().AddRange(details);

            await _context.SaveChangesAsync();

            var top2 = await _service.GetTopAsync(2);

            Assert.Equal(2, top2.Count);
            Assert.Equal("u1", top2[0].Username);
            Assert.Equal(50, top2[0].TotalScore);
            Assert.Equal("u2", top2[1].Username);
            Assert.Equal(40, top2[1].TotalScore);
        }

        [Fact]
        public async Task EnsurePrivateUsersAreExcluded()
        {
            var u1 = new ApplicationUser { Id = "u1", UserName = "public1" };
            var u2 = new ApplicationUser { Id = "u2", UserName = "private1" };

            _context.Users.AddRange(u1, u2);

            _context.Set<UserDetails>().AddRange(
                new UserDetails { UserId = "u1", TotalScore = 30, IsPrivate = false },
                new UserDetails { UserId = "u2", TotalScore = 100, IsPrivate = true }
            );

            await _context.SaveChangesAsync();

            var top = await _service.GetTopAsync(10);

            Assert.DoesNotContain(top, e => e.Username == "private1");
            Assert.Contains(top, e => e.Username == "public1");
        }
    }
}
