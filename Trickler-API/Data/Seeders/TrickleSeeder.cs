using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trickler_API.Data;
using Trickler_API.Models;

namespace Trickler_API.Data.Seeders
{
    public static class TrickleSeeder
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<TricklerDbContext>();
            var logger = services.GetService<ILoggerFactory>()?.CreateLogger(nameof(TrickleSeeder));

            logger?.LogInformation("Starting trickle seed.");

            try
            {
                await context.Database.MigrateAsync();
                logger?.LogInformation("Database migration completed for trickles.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Migration failed. Aborting trickle seed.");
                return;
            }

            const string trickleTitle = "Trickle 1 Title";

            if (await context.Trickles.AnyAsync(t => t.Title == trickleTitle))
            {
                logger?.LogInformation("Trickle '{Title}' already exists.", trickleTitle);
                return;
            }

            var trickle = new Trickle
            {
                Title = trickleTitle,
                Text = "Trickle 1 Text. Answer is \"trickle 1 answer\"",
                QuestionType = QuestionType.Single,
                Score = 0,
                RewardText = string.Empty,
                AttemptsPerTrickle = -1,
                Availability = new Availability
                {
                    Type = AvailabilityType.Weekly,
                    DaysOfWeek = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]
                },
                Answers =
                [
                    new Answer { AnswerText = "trickle 1 answer" }
                ]
            };

            await context.Trickles.AddAsync(trickle);
            await context.SaveChangesAsync();

            logger?.LogInformation("Seeded trickle '{Title}' with id {Id}.", trickle.Title, trickle.Id);
        }
    }
}
