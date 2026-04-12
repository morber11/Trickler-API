using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Trickler_API.Constants;
using Trickler_API.Models;

namespace Trickler_API.Data.Seeders
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
            const string adminUsername = "admin";

            await EnsureRoleAsync(roleManager, logger, adminRole);
            await EnsureRoleAsync(roleManager, logger, userRole);

            var env = services.GetService<IHostEnvironment>();
            if (env?.IsDevelopment() is true)
            {
                var userPassword = config["DEFAULT_USER_PASSWORD"] ?? "Defaultuserpassword1!";
                var seededUser = await EnsureUserAsync(
                    userManager,
                    logger,
                    "user",
                    "user@trickler.com",
                    userPassword,
                    userRole);

                if (seededUser is null)
                {
                    logger?.LogError("Dev user seeding failed.");
                }
            }

            var admin = await EnsureAdminUserAsync(
                userManager,
                logger,
                adminUsername,
                adminEmail,
                adminPassword,
                adminRole);

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

        private static async Task<ApplicationUser?> EnsureAdminUserAsync(
            UserManager<ApplicationUser> userManager,
            ILogger? logger,
            string username,
            string email,
            string password,
            string role)
        {
            var user = await userManager.FindByNameAsync(username) ?? await userManager.FindByEmailAsync(email);

            if (user is not null)
            {
                logger?.LogInformation("Admin user already exists");
                return user;
            }

            var newUser = new ApplicationUser
            {
                UserName = username,
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

        private static async Task<ApplicationUser?> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            ILogger? logger,
            string username,
            string email,
            string password,
            string role)
        {
            var user = await userManager.FindByNameAsync(username) ?? await userManager.FindByEmailAsync(email);

            if (user is not null)
            {
                logger?.LogInformation("User already exists");
                return user;
            }

            var newUser = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(newUser, password);
            if (!createResult.Succeeded)
            {
                logger?.LogError("Failed to create user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return null;
            }

            logger?.LogInformation("Created user '{Email}'.", email);

            var roleResult = await userManager.AddToRoleAsync(newUser, role);
            if (!roleResult.Succeeded)
            {
                logger?.LogError("Failed to add user to role '{Role}': {Errors}", role, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
            else
            {
                logger?.LogInformation("Added user '{Email}' to role '{Role}'.", email, role);
            }

            return newUser;
        }
    }
}
