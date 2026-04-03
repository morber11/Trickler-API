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
                return new UserDetailsDto(existing.Id, existing.UserId, existing.TotalScore);
            }

            var entity = new UserDetails
            {
                UserId = userId,
                TotalScore = 0
            };

            await set.AddAsync(entity);
            await _context.SaveChangesAsync();

            return new UserDetailsDto(entity.Id, entity.UserId, entity.TotalScore);
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
                return new UserDetailsDto(existing.Id, existing.UserId, existing.TotalScore);
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
                    return new UserDetailsDto(reloaded.Id, reloaded.UserId, reloaded.TotalScore);
                }
                throw;
            }

            return new UserDetailsDto(entity.Id, entity.UserId, entity.TotalScore);
        }
    }
}
