using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Trickler_API.Constants;
using Trickler_API.DTO;
using Trickler_API.Models;

namespace Trickler_API.Services
{
    public class AccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        UserTricklesService userTricklesService,
        ILogger<AccountService> logger)
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly UserTricklesService _userTricklesService = userTricklesService;
        private readonly ILogger<AccountService> _logger = logger;

        public async Task<SignInResultDto> ValidateAndSignInAsync(string identifier, string password)
        {
            var user = await _userManager.FindByNameAsync(identifier) ?? await _userManager.FindByEmailAsync(identifier);

            if (user is null)
            {
                return new SignInResultDto(
                    false,
                    MessageConstants.Auth.InvalidCredentials,
                    null,
                    []);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                return new SignInResultDto(
                    false,
                    MessageConstants.Auth.InvalidCredentials,
                    null,
                    []);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            var roles = await _userManager.GetRolesAsync(user);

            _logger.LogInformation("User {Identifier} logged in successfully", identifier);

            var userDto = new SignedInUserDto(user.Id, user.UserName, user.Email);
            return new SignInResultDto(
                true,
                null,
                userDto,
                roles);
        }

        public async Task<(bool Succeeded, string? ErrorMessage, ApplicationUser? User)> RegisterAsync(string username, string email, string password)
        {
            var existingByName = await _userManager.FindByNameAsync(username);
            var existingByEmail = await _userManager.FindByEmailAsync(email);

            if (existingByName is not null || existingByEmail is not null)
            {
                return (false, MessageConstants.Account.UserAlreadyExists, null);
            }

            var user = new ApplicationUser
            {
                UserName = username,
                Email = email
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Registration failed for {Email}: {Errors}", email, errors);

                return (false, errors, null);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, RoleConstants.User);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to assign user role to {Email}: {Errors}", email, errors);

                return (false, errors, null);
            }

            _logger.LogInformation("User {Email} registered successfully with user role", email);
            return (true, null, user);
        }

        public async Task<(bool Succeeded, string? ErrorMessage)> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return (false, MessageConstants.Account.UserNotFound);
            }

            var oidcSub = (await _userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == "sub")?.Value;
            if (!string.IsNullOrEmpty(oidcSub))
            {
                return (false, MessageConstants.Account.CannotChangePasswordForOidcUsers);
            }

            var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }

            _logger.LogInformation("User {UserId} changed password", userId);
            return (true, null);
        }

        public async Task<object> GetProfileAsync(ClaimsPrincipal principal)
        {
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new { isAuthenticated = false };
            }

            var user = await _userManager.FindByIdAsync(userId);
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = principal.FindFirst(ClaimTypes.Name)?.Value ?? user?.UserName;
            var oidcSub = principal.FindFirst("sub")?.Value;
            var authMethod = !string.IsNullOrEmpty(oidcSub) ? "oidc" : "local";

            var roles = user is not null ? await _userManager.GetRolesAsync(user) : Array.Empty<string>();

            var solvedCount = await _userTricklesService.CountSolvedAsync(userId);
            var recentSolvedEntities = await _userTricklesService.GetRecentSolvedAsync(userId, take: 5);
            var recentSolved = recentSolvedEntities.Select(ut => new { ut.TrickleId, ut.SolvedAt, ut.RewardCode }).ToList();

            return new
            {
                userId,
                email = email ?? user?.Email,
                name,
                authenticationMethod = authMethod,
                isAuthenticated = principal.Identity?.IsAuthenticated ?? false,
                emailConfirmed = user?.EmailConfirmed ?? false,
                roles,
                solvedCount,
                recentSolved
            };
        }

        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User signed out via AccountService");
        }
    }
}
