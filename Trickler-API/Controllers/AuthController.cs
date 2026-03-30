using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Trickler_API.Constants;
using Trickler_API.DTO;
using Trickler_API.Services;

namespace Trickler_API.Controllers
{
    /// <summary>
    /// Handles API authentication flows (JWT, oidc).
    /// Does not work presently
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [EnableRateLimiting("sliding")]
    public class AuthController(
        AuthService authService,
        AccountService accountService,
        ILogger<AuthController> logger) : ControllerBase
    {
        private readonly AuthService _authService = authService;
        private readonly AccountService _accountService = accountService;
        private readonly ILogger<AuthController> _logger = logger;


        /// <summary>
        /// Initiate OIDC login flow.
        /// </summary>
        /// <param name="returnUrl">Optional return URL after login.</param>
        /// <returns>Challenge result to start OIDC authentication.</returns>
        [HttpGet("login-oidc")]
        [AllowAnonymous]
        public IActionResult LoginOidc(string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(LoginOidcCallback), new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            _logger.LogInformation("User initiated OIDC login");
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// OIDC callback handler - confirms successful OIDC authentication.
        /// </summary>
        /// <param name="returnUrl">Optional return URL provided during login.</param>
        /// <returns>200 with authentication details or redirect on failure.</returns>
        [HttpGet("login-oidc-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginOidcCallback(string? returnUrl = null)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Redirect("/login");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var oidcSub = User.FindFirst("sub")?.Value;

            _logger.LogInformation("User authenticated via OIDC, subject: {Subject}, email: {Email}", oidcSub, email);

            if (!string.IsNullOrEmpty(email))
            {
                var (_, _) = await _authService.EnsureLocalUserForOidcAsync(email, oidcSub);
                // logging handled in service; CreatedUser may be null on failure
            }

            return Ok(new LoginResponse(
                MessageConstants.Auth.OidcLoginSuccessful,
                userId,
                email,
                "oidc",
                returnUrl ?? "/",
                null));
        }
    }
}
