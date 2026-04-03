using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Trickler_API.Constants;
using Trickler_API.DTO;
using Trickler_API.Services;

namespace Trickler_API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    [EnableRateLimiting("token")]
    /// <summary>
    /// Provides endpoints to create, read, update and delete trickles.
    /// </summary>
    public class TricklerController(TricklerService tricklerService) : ControllerBase
    {
        private readonly TricklerService _tricklerService = tricklerService;

        /// <summary>
        /// Returns all trickles that are currently available based on their availability settings.
        /// </summary>
        /// <returns>200 with list of available trickles (without answers).</returns>
        [HttpGet("available")]
        [Authorize(Roles = RoleConstants.AdminOrUser)]
        public async Task<IActionResult> GetAvailable()
        {
            var trickles = await _tricklerService.GetAvailableTricklesAsync();
            return Ok(trickles);
        }

        [HttpGet("")]
        [Authorize(Roles = RoleConstants.Admin)]
        /// <summary>
        /// Returns all trickles including answers (Admin only).
        /// </summary>
        /// <returns>200 with list of trickles and their answers.</returns>
        public async Task<IActionResult> GetAll()
        {
            var trickles = await _tricklerService.GetAllTricklesAsync();
            return Ok(trickles);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = RoleConstants.AdminOrUser)]
        /// <summary>
        /// Returns a trickle by id.
        /// </summary>
        /// <param name="id">Trickle identifier.</param>
        /// <returns>200 with trickle, 404 if not found.</returns>
        public async Task<IActionResult> GetById(int id)
        {
            var trickle = await _tricklerService.GetTrickleByIdAsync(id);

            if (trickle is null)
            {
                return NotFound(new MessageResponse(string.Format(MessageConstants.Trickle.TrickleNotFound, id)));
            }

            return Ok(trickle);
        }

        [HttpPost("")]
        [Authorize(Roles = RoleConstants.Admin)]
        /// <summary>
        /// Creates a new trickle with answers (Admin only).
        /// </summary>
        /// <param name="request">Creation request containing text and answers.</param>
        /// <returns>201 with created trickle and answers.</returns>
        public async Task<IActionResult> Create([FromBody] CreateTrickleRequest request)
        {
            var dto = await _tricklerService.CreateTrickleAsync(
                request.Title,
                request.Text,
                request.Answers,
                request.Availability,
                request.Score,
                request.RewardText,
                request.AttemptsPerTrickle);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        /// <summary>
        /// Updates an existing trickle.
        /// </summary>
        /// <param name="id">Trickle identifier.</param>
        /// <param name="request">Update request containing new text.</param>
        /// <returns>200 with updated trickle, 404 if not found.</returns>
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTrickleRequest request)
        {
            var dto = await _tricklerService.UpdateTrickleAsync(
                id,
                request.Title,
                request.Text,
                request.Answers,
                request.Availability,
                request.Score,
                request.RewardText,
                request.AttemptsPerTrickle);
            return Ok(dto);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        /// <summary>
        /// Deletes a trickle by id.
        /// </summary>
        /// <param name="id">Trickle identifier.</param>
        /// <returns>204 on success, 404 if not found.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            await _tricklerService.DeleteTrickleAsync(id);
            return NoContent();
        }
    }
}
