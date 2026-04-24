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
    public class AnswersController(AnswersService answersService, TricklerService tricklerService) : ControllerBase
    {
        private readonly AnswersService _answersService = answersService;
        private readonly TricklerService _tricklerService = tricklerService;


        /// <summary>
        /// Submit an answer (persists attempts, issues reward on first correct solve).
        /// </summary>
        [HttpPost("submit")]
        [Authorize(Roles = RoleConstants.AdminOrUser)]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new MessageResponse(MessageConstants.Auth.UserNotAuthenticated));
            }

            var result = await _answersService.SubmitAnswerAsync(request.TrickleId, request.Answer, userId);

            if (result.Result == SubmitAttemptResultType.Locked)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new MessageResponse(MessageConstants.Answers.AttemptLimitReached));
            }

            var progress = await _tricklerService.GetAvailableTricklesForUserAsync(userId);
            return Ok(progress with { Result = result.Result.ToString().ToLowerInvariant() });
        }

        [HttpPost("/api/v1/trickler/{id}/attempt")]
        [Authorize(Roles = RoleConstants.AdminOrUser)]
        public async Task<IActionResult> SubmitAttempt(int id, [FromBody] SubmitAttemptRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new MessageResponse(MessageConstants.Auth.UserNotAuthenticated));
            }

            var result = await _answersService.SubmitAnswerAsync(id, request.Answer, userId);
            if (result.Result == SubmitAttemptResultType.Locked)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new MessageResponse(MessageConstants.Answers.AttemptLimitReached));
            }

            var progress = await _tricklerService.GetAvailableTricklesForUserAsync(userId);
            return Ok(progress with { Result = result.Result.ToString().ToLowerInvariant() });
        }
    }
}