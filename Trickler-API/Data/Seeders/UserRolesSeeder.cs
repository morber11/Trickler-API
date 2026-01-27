using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trickler_API.Constants;
using Trickler_API.Models;

namespace Trickler_API.Data
{
    public static class UserRolesSeeder
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<TricklerDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var config = services.GetRequiredService<IConfiguration>();

            var logger = services.GetService<ILoggerFactory>()?.CreateLogger(nameof(UserRolesSeeder));

            logger?.LogInformation("Starting database seed.");

            // it really doesn't matter if this is hardcoded because it will be reset in production anyway
            var adminPassword = config["DEFAULT_ADMIN_PASSWORD"] ?? "Defaultadminpassword1!";

            try
            {
                await context.Database.MigrateAsync();
                logger?.LogInformation("Database migration completed.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Migration failed. Aborting seed.");
                return;
            }

            const string adminRole = RoleConstants.Admin;
            const string userRole = RoleConstants.User;
            const string adminEmail = "admin@trickler.com";

            await EnsureRoleAsync(roleManager, logger, adminRole);
            await EnsureRoleAsync(roleManager, logger, userRole);

            var admin = await EnsureAdminUserAsync(userManager, logger, adminEmail, adminPassword, adminRole);

            if (admin is null)
            {
                logger?.LogError("Admin user seeding failed.");
            }
            else
            {
                logger?.LogInformation("Seeding completed successfully.");
            }
        }

        private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, ILogger? logger, string role)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                logger?.LogInformation("Role '{Role}' already exists.", role);
                return;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (result.Succeeded)
            {
                logger?.LogInformation("Created role '{Role}'.", role);
            }
            else
            {
                logger?.LogError("Failed to create role '{Role}': {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        private static async Task<ApplicationUser?> EnsureAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger? logger, string email, string password, string role)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user is not null)
            {
                logger?.LogInformation("Admin user '{Email}' already exists.", email);
                return user;
            }

            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(newUser, password);
            if (!createResult.Succeeded)
            {
                logger?.LogError("Failed to create admin user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return null;
            }

            logger?.LogInformation("Created admin user '{Email}'.", email);

            var roleResult = await userManager.AddToRoleAsync(newUser, role);
            if (!roleResult.Succeeded)
            {
                logger?.LogError("Failed to add admin user to role '{Role}': {Errors}", role, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
            else
            {
                logger?.LogInformation("Added admin user '{Email}' to role '{Role}'.", email, role);
            }

            return newUser;
        }
    }
}
