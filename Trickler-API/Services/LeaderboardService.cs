using Microsoft.EntityFrameworkCore;
using Trickler_API.Data;
using Trickler_API.DTO;

namespace Trickler_API.Services
{
    public class LeaderboardService(TricklerDbContext context)
    {
        private readonly TricklerDbContext _context = context;

        public async Task<List<LeaderboardEntryDto>> GetTopAsync(int take = 10)
        {
            if (take <= 0) take = 10;

            var query = from ud in _context.UserDetails.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on ud.UserId equals u.Id
                        where !ud.IsPrivate
                        orderby ud.TotalScore descending
                        select new LeaderboardEntryDto(u.UserName ?? string.Empty, ud.TotalScore);

            return await query.Take(take).ToListAsync();
        }
    }
}
