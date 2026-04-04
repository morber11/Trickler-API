using Microsoft.EntityFrameworkCore;
using Trickler_API.Data;
using Trickler_API.DTO;
using Trickler_API.Models;

namespace Trickler_API.Services
{
    public class UserDetailsService(TricklerDbContext context)
    {
        private readonly TricklerDbContext _context = context;

        private async Task<string> GetUserNameForIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return string.Empty;

            var appUser = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);

            return appUser?.UserName ?? string.Empty;
        }

        public async Task<UserDetailsResponseDto> AddUserDetailsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException(null, nameof(userId));

            var set = _context.Set<UserDetails>();
            var existing = await set.SingleOrDefaultAsync(u => u.UserId == userId);

            if (existing is not null)
            {
                var existingUserName = await GetUserNameForIdAsync(userId);
                return new UserDetailsResponseDto(
                    existing.Id,
                    existing.UserId,
                    existing.TotalScore,
                    existing.IsPrivate,
                    existingUserName,
                    existing.CurrentScore);
            }

            var entity = new UserDetails
            {
                UserId = userId,
                TotalScore = 0,
                CurrentScore = 0,
                IsPrivate = false
            };

            await set.AddAsync(entity);
            await _context.SaveChangesAsync();

            var newUserName = await GetUserNameForIdAsync(userId);
            return new UserDetailsResponseDto(
                entity.Id,
                entity.UserId,
                entity.TotalScore,
                entity.IsPrivate,
                newUserName,
                entity.CurrentScore);
        }



        public async Task<UserDetailsResponseDto?> GetOrCreateUserDetailsAsync(string userId, string? currentUserId, bool isAdmin)
        {
            if (!isAdmin)
            {
                if (string.IsNullOrWhiteSpace(currentUserId)) return null;
                if (!string.Equals(currentUserId, userId, StringComparison.Ordinal)) return null;
            }

            var set = _context.Set<UserDetails>();
            var existing = await set.SingleOrDefaultAsync(u => u.UserId == userId);

            if (existing is not null)
            {
                var existingUserName = await GetUserNameForIdAsync(userId);
                return new UserDetailsResponseDto(
                    existing.Id,
                    existing.UserId,
                    existing.TotalScore,
                    existing.IsPrivate,
                    existingUserName,
                    existing.CurrentScore);
            }

            var entity = new UserDetails
            {
                UserId = userId,
                TotalScore = 0,
                CurrentScore = 0
            };

            await set.AddAsync(entity);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Possible unique constraint race - try to read existing record
                var reloaded = await set.SingleOrDefaultAsync(u => u.UserId == userId);
                if (reloaded is not null)
                {
                    var reloadedUserName = await GetUserNameForIdAsync(userId);
                    return new UserDetailsResponseDto(
                        reloaded.Id,
                        reloaded.UserId,
                        reloaded.TotalScore,
                        reloaded.IsPrivate,
                        reloadedUserName,
                        reloaded.CurrentScore);
                }
                throw;
            }

            var createdUserName = await GetUserNameForIdAsync(userId);
            return new UserDetailsResponseDto(
                entity.Id,
                entity.UserId,
                entity.TotalScore,
                entity.IsPrivate,
                createdUserName,
                entity.CurrentScore);
        }

        public async Task UpdateUserScoreAsync(string userId, int scoreToAdd)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException(null, nameof(userId));
            if (scoreToAdd <= 0) return;

            var set = _context.Set<UserDetails>();
            var existing = await set.FirstOrDefaultAsync(u => u.UserId == userId);

            if (existing is null)
            {
                var entity = new UserDetails
                {
                    UserId = userId,
                    TotalScore = scoreToAdd,
                    CurrentScore = scoreToAdd,
                    IsPrivate = false
                };
                await set.AddAsync(entity);
            }
            else
            {
                existing.TotalScore += scoreToAdd;
                existing.CurrentScore += scoreToAdd;
                _context.Entry(existing).State = EntityState.Modified;
            }
        }

        public async Task MergeUserScoreFromDatabaseAsync(string userId, int scoreToAdd)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException(null, nameof(userId));
            if (scoreToAdd <= 0) return;

            var set = _context.Set<UserDetails>();
            var reloaded = await set.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);

            if (reloaded is not null)
            {
                var local = _context.UserDetails.Local.FirstOrDefault(u => u.UserId == userId);
                if (local is not null)
                {
                    local.TotalScore = reloaded.TotalScore + scoreToAdd;
                    local.CurrentScore = reloaded.CurrentScore + scoreToAdd;
                }
                else
                {
                    reloaded.TotalScore += scoreToAdd;
                    reloaded.CurrentScore += scoreToAdd;
                    _context.UserDetails.Attach(reloaded);
                    _context.Entry(reloaded).State = EntityState.Modified;
                }
            }
            else
            {
                var newDetails = new UserDetails
                {
                    UserId = userId,
                    TotalScore = scoreToAdd,
                    CurrentScore = scoreToAdd,
                    IsPrivate = false
                };
                _context.UserDetails.Add(newDetails);
            }
        }
    }
}
