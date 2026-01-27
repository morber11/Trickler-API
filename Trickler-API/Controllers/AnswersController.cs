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
    public class AnswersController(AnswersService answersService) : ControllerBase
    {
        private readonly AnswersService _answersService = answersService;

        /// <summary>
        /// Verify an answer for a trickle.
        /// </summary>
        /// <param name="request">Contains trickle id and answer text.</param>
        /// <returns>200 with { correct = true/false }.</returns>
        [HttpPost("verify")]
        [Authorize(Roles = RoleConstants.AdminOrUser)]
        public async Task<IActionResult> VerifyAnswer([FromBody] VerifyAnswerRequest request)
        {
            var trickleExists = await _answersService.VerifyAnswerAsync(request.TrickleId, request.Answer);
            return Ok(new { correct = trickleExists });
        }

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
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _answersService.SubmitAnswerAsync(request.TrickleId, request.Answer, userId);

            if (result.AttemptsLeft <= 0 && !result.Correct)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Attempt limit reached for today" });
            }

            return Ok(new SubmitAnswerResponse(result.Correct, result.RewardCode, result.AttemptsLeft));
        }
    }
}