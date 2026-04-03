using Microsoft.EntityFrameworkCore;
using Trickler_API.Data;
using Trickler_API.Models;
using Trickler_API.Services;

namespace Trickler_API.Tests.Services
{
    public class UserDetailsServiceTests : IDisposable
    {
        private readonly TricklerDbContext _context;
        private readonly UserDetailsService _service;

        public UserDetailsServiceTests()
        {
            var options = new DbContextOptionsBuilder<TricklerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TricklerDbContext(options);
            _service = new UserDetailsService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task AddUserDetailsAsync_CreatesRecord_WithZeroTotalScore()
        {
            var userId = "user-1";
            var dto = await _service.AddUserDetailsAsync(userId);

            Assert.NotNull(dto);
            Assert.Equal(userId, dto.UserId);
            Assert.Equal(0, dto.TotalScore);

            var entity = await _context.Set<UserDetails>().SingleAsync(u => u.UserId == userId);
            Assert.Equal(userId, entity.UserId);
            Assert.Equal(0, entity.TotalScore);
        }

        [Fact]
        public async Task AddUserDetailsAsync_Idempotent_ReturnsExistingRecord()
        {
            var userId = "user-2";
            var first = await _service.AddUserDetailsAsync(userId);
            var second = await _service.AddUserDetailsAsync(userId);

            Assert.Equal(first.Id, second.Id);
            Assert.Equal(first.UserId, second.UserId);
        }

    }
}
