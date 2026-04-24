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

            if (result.AttemptsLeft <= 0 && !result.IsSolved)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new MessageResponse(MessageConstants.Answers.AttemptLimitReached));
            }

            return Ok(new SubmitAnswerResponse(
                result.IsSolved,
                result.SolvedAt,
                result.RewardCode,
                result.AttemptsLeft,
                result.CurrentScore));
        }
    }
}