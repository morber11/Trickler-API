using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Trickler_API.Constants;
using Trickler_API.DTO;
using Trickler_API.Services;

namespace Trickler_API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    [EnableRateLimiting("token")]
    public class UserTricklesController(UserTricklesService userTricklesService) : ControllerBase
    {
        private readonly UserTricklesService _userTricklesService = userTricklesService;

        [HttpGet("solved/{userId}")]
        [Authorize(Roles = RoleConstants.AdminOrUser)]
        public async Task<IActionResult> GetUserSolvedTrickles(string userId, [FromQuery] int take = 30)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(new MessageResponse(MessageConstants.Auth.UserNotAuthenticated));
            }

            if (!User.IsInRole(RoleConstants.Admin) && !string.Equals(currentUserId, userId, StringComparison.Ordinal))
            {
                return Forbid();
            }

            var solved = await _userTricklesService.GetSolvedByUserAsync(userId, take);
            var dtos = solved.Select(ut => new SolvedTrickleDto(
                ut.TrickleId,
                ut.Trickle?.Title ?? string.Empty,
                ut.SolvedAt,
                ut.RewardCode,
                ut.CurrentScore
            ));

            return Ok(dtos);
        }

        [HttpGet("{userId}/has-solved/{trickleId}")]
        [Authorize(Roles = RoleConstants.AdminOrUser)]
        public async Task<IActionResult> HasUserSolvedTrickle(string userId, int trickleId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(new MessageResponse(MessageConstants.Auth.UserNotAuthenticated));
            }

            if (!User.IsInRole(RoleConstants.Admin) && !string.Equals(currentUserId, userId, StringComparison.Ordinal))
            {
                return Forbid();
            }

            var solved = await _userTricklesService.HasUserSolvedTrickleAsync(trickleId, userId);
            return Ok(solved);
        }
    }
}
