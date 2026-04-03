using Microsoft.EntityFrameworkCore;
using Trickler_API.Data;
using Trickler_API.DTO;
using Trickler_API.Models;

namespace Trickler_API.Services
{
    public class UserDetailsService(TricklerDbContext context)
    {
        private readonly TricklerDbContext _context = context;

        public async Task<UserDetailsDto> AddUserDetailsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException(null, nameof(userId));

            var set = _context.Set<UserDetails>();
            var existing = await set.SingleOrDefaultAsync(u => u.UserId == userId);

            if (existing is not null)
            {
                return new UserDetailsDto(
                    existing.Id,
                    existing.UserId,
                    existing.TotalScore,
                    existing.IsPrivate);
            }

            var entity = new UserDetails
            {
                UserId = userId,
                TotalScore = 0,
                IsPrivate = false
            };

            await set.AddAsync(entity);
            await _context.SaveChangesAsync();

            return new UserDetailsDto(
                entity.Id,
                entity.UserId,
                entity.TotalScore,
                entity.IsPrivate);
        }



        public async Task<UserDetailsDto?> GetOrCreateUserDetailsAsync(string userId, string? currentUserId, bool isAdmin)
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
                return new UserDetailsDto(existing.Id, existing.UserId, existing.TotalScore, existing.IsPrivate);
            }

            var entity = new UserDetails
            {
                UserId = userId,
                TotalScore = 0
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
                    return new UserDetailsDto(
                        reloaded.Id,
                        reloaded.UserId,
                        reloaded.TotalScore,
                        reloaded.IsPrivate);
                }
                throw;
            }

            return new UserDetailsDto(
                entity.Id,
                entity.UserId,
                entity.TotalScore,
                entity.IsPrivate);
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
                    IsPrivate = false
                };
                await set.AddAsync(entity);
            }
            else
            {
                existing.TotalScore += scoreToAdd;
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
                }
                else
                {
                    reloaded.TotalScore += scoreToAdd;
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
                    IsPrivate = false
                };
                _context.UserDetails.Add(newDetails);
            }
        }
    }
}
