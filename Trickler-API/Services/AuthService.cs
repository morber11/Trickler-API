using Microsoft.AspNetCore.Identity;
using Trickler_API.Models;

namespace Trickler_API.Services
{
    public class AuthService(UserManager<ApplicationUser> userManager, ILogger<AuthService> logger)
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ILogger<AuthService> _logger = logger;

        public async Task<IList<string>> GetRolesForUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return Array.Empty<string>();
            }

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<(bool Created, ApplicationUser? User)> EnsureLocalUserForOidcAsync(string email, string? oidcSub)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(oidcSub))
            {
                return (false, null);
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser is not null)
            {
                return (false, existingUser);
            }

            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email
            };

            var result = await _userManager.CreateAsync(newUser);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to create local user for OIDC user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return (false, null);
            }

            _logger.LogInformation("Created local user record for OIDC user {Email}", email);
            return (true, newUser);
        }
    }
}