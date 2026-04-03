using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
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
    [EnableRateLimiting("sliding")]
    public class AccountController(
        AccountService accountService,
        ILogger<AccountController> logger) : ControllerBase
    {
        private readonly AccountService _accountService = accountService;
        private readonly ILogger<AccountController> _logger = logger;

        /// <summary>
        /// Register a new local user with username and password.
        /// </summary>
        /// <param name="request">Registration details (email, password).</param>
        /// <returns>200 on success, 400 for invalid input, 500 on server error.</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new MessageResponse(MessageConstants.Account.EmailAndPasswordRequired));
            }

            var (Succeeded, ErrorMessage, CreatedUser) = await _accountService.RegisterAsync(request.Email, request.Password);
            if (!Succeeded)
            {
                return BadRequest(new ErrorResponse(MessageConstants.Account.RegistrationFailed, ErrorMessage));
            }

            return Ok(new RegisterResponse(MessageConstants.Account.UserRegisteredSuccessfully, CreatedUser?.Id));
        }

        /// <summary>
        /// Process login request with email and password.
        /// </summary>
        /// <param name="request">Login credentials (email, password).</param>
        /// <param name="returnUrl">Optional return URL after successful login.</param>
        /// <returns>200 with auth details on success, 401 on invalid credentials, 500 on server error.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, [FromQuery] string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new MessageResponse(MessageConstants.Account.EmailAndPasswordRequired));
            }

            var (succeeded, errorMessage, user, roles) = await _accountService.ValidateAndSignInAsync(request.Email, request.Password);
            if (!succeeded)
            {
                _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                return Unauthorized(new MessageResponse(errorMessage ?? MessageConstants.Auth.InvalidCredentials));
            }

            _logger.LogInformation("User {Email} logged in successfully", request.Email);

            return Ok(new LoginResponse(
                MessageConstants.Auth.LoginSuccessful,
                user!.Id,
                user.Email,
                "local",
                returnUrl ?? "/",
                roles));
        }

        /// <summary>
        /// Get current user profile.
        /// </summary>
        /// <returns>200 with profile, 401 if not authenticated, 500 on server error.</returns>
        [HttpGet("profile")]
        [Authorize(Roles = RoleConstants.AdminOrUser)]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new MessageResponse(MessageConstants.Auth.UserNotAuthenticated));
            }

            var profile = await _accountService.GetProfileAsync(User);
            return Ok(profile);
        }

        /// <summary>
        /// Change password for local users.
        /// </summary>
        /// <param name="request">Old and new password values.</param>
        /// <returns>200 on success, 400 for invalid request or failure, 401 if not authenticated, 500 on server error.</returns>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new MessageResponse(MessageConstants.Auth.UserNotAuthenticated));
            }

            var oidcSub = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(oidcSub))
            {
                return BadRequest(new MessageResponse(MessageConstants.Account.CannotChangePasswordForOidcUsers));
            }

            if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new MessageResponse(MessageConstants.Account.OldAndNewPasswordRequired));
            }

            var (Succeeded, ErrorMessage) = await _accountService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
            if (!Succeeded)
            {
                return BadRequest(new ErrorResponse(MessageConstants.Account.FailedToChangePassword, ErrorMessage));
            }

            return Ok(new MessageResponse(MessageConstants.Account.PasswordChangedSuccessfully));
        }

        /// <summary>
        /// Logout endpoint - clears authentication.
        /// </summary>
        /// <param name="returnUrl">Optional return URL after logout.</param>
        /// <returns>204 No Content on success.</returns>
        [HttpGet("logout")]
        [Authorize]
#pragma warning disable IDE0060 // Remove unused parameter - used in frontend to redirect post logout
        public async Task<IActionResult> Logout(string? returnUrl = null)
#pragma warning restore IDE0060 
        {
            _logger.LogInformation("User initiated logout");

            try
            {
                await _accountService.SignOutAsync();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation("Logout completed for request {Path}", Request.Path);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed for request {Path}", Request.Path);
                throw;
            }
        }
    }
}
