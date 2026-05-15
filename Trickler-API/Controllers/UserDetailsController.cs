using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Trickler_API.Constants;
using Trickler_API.DTO;
using Trickler_API.Services;

namespace Trickler_API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class UserDetailsController(UserDetailsService userDetailsService, ILogger<UserDetailsController> logger) : ControllerBase
    {
        private readonly UserDetailsService _userDetailsService = userDetailsService;
        private readonly ILogger<UserDetailsController> _logger = logger;

        [HttpPost("{userId}")]
        [Authorize(Roles = RoleConstants.AdminOrUser)]
        public async Task<IActionResult> GetOrCreateUserDetails(string userId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole(RoleConstants.Admin);

            var dto = await _userDetailsService.GetOrCreateUserDetailsAsync(userId, currentUserId, isAdmin);
            if (dto is null)
            {
                return NotFound(new MessageResponse(MessageConstants.Account.UserNotFound));
            }

            return Ok(dto);
        }

        [HttpPut("{userId}/visibility")]
        [Authorize(Roles = RoleConstants.AdminOrUser)]
        public async Task<IActionResult> UpdateVisibility(string userId, [FromBody] UpdateUserVisibilityRequest request)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole(RoleConstants.Admin);

            if (!isAdmin && !string.Equals(currentUserId, userId, StringComparison.Ordinal))
            {
                return Forbid();
            }

            var dto = await _userDetailsService.UpdateUserPrivacyAsync(userId, request.IsPrivate);
            if (dto is null)
            {
                return NotFound(new MessageResponse(MessageConstants.Account.UserNotFound));
            }

            return Ok(dto);
        }
    }
}
