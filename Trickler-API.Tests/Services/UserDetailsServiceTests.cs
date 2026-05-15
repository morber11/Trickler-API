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

        [Fact]
        public async Task AddUserDetailsAsync_CreatesRecord_WithUsernameFromUser()
        {
            var userId = "user-with-username";
            var appUser = new ApplicationUser { Id = userId, UserName = "bob", Email = "bob@example.com" };
            _context.Users.Add(appUser);
            await _context.SaveChangesAsync();

            var dto = await _service.AddUserDetailsAsync(userId);

            Assert.NotNull(dto);
            Assert.Equal(userId, dto.UserId);
            Assert.Equal("bob", dto.Username);

            var entity = await _context.Set<UserDetails>().SingleAsync(u => u.UserId == userId);
            Assert.Equal(userId, entity.UserId);
        }

        [Fact]
        public async Task GetOrCreateUserDetailsAsync_ReturnsExisting_WithUsername()
        {
            var userId = "existing-user";
            var appUser = new ApplicationUser { Id = userId, UserName = "alice", Email = "alice@example.com" };
            _context.Users.Add(appUser);

            var details = new UserDetails { UserId = userId, TotalScore = 5, IsPrivate = false };
            _context.Set<UserDetails>().Add(details);
            await _context.SaveChangesAsync();

            var dto = await _service.GetOrCreateUserDetailsAsync(userId, userId, true);

            Assert.NotNull(dto);
            Assert.Equal(userId, dto!.UserId);
            Assert.Equal("alice", dto.Username);
        }

        [Fact]
        public async Task UpdateUserPrivacyAsync_ExistingUser_UpdatesIsPrivate()
        {
            var userId = "user-private";
            _context.Set<UserDetails>().Add(new UserDetails
            {
                UserId = userId,
                TotalScore = 12,
                CurrentScore = 4,
                IsPrivate = false,
            });
            await _context.SaveChangesAsync();

            var dto = await _service.UpdateUserPrivacyAsync(userId, true);

            Assert.NotNull(dto);
            Assert.True(dto!.IsPrivate);
            Assert.Equal(12, dto.TotalScore);

            var entity = await _context.Set<UserDetails>().SingleAsync(u => u.UserId == userId);
            Assert.True(entity.IsPrivate);
        }

        [Fact]
        public async Task UpdateUserPrivacyAsync_MissingUser_ReturnsNull()
        {
            var dto = await _service.UpdateUserPrivacyAsync("missing-user", true);

            Assert.Null(dto);
        }

    }
}
