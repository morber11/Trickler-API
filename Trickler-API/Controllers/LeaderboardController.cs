using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trickler_API.Constants;
using Trickler_API.Services;

namespace Trickler_API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = RoleConstants.AdminOrUser)]
    public class LeaderboardController(LeaderboardService leaderboardService, ILogger<LeaderboardController> logger) : ControllerBase
    {
        private readonly LeaderboardService _leaderboardService = leaderboardService;
        private readonly ILogger<LeaderboardController> _logger = logger;

        [HttpGet]
        public async Task<IActionResult> GetTop([FromQuery] int take = 10)
        {
            var entries = await _leaderboardService.GetTopAsync(take);
            return Ok(entries);
        }
    }
}
