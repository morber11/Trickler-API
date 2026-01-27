using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Trickler_API.Models;

namespace Trickler_API.Data
{
    public class TricklerDbContext(DbContextOptions<TricklerDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public virtual DbSet<Trickle> Trickles { get; set; }
        public virtual DbSet<Answer> Answers { get; set; }
        public virtual DbSet<Availability> Availabilities { get; set; }
        public virtual DbSet<UserTrickle> UserTrickles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Trickle>(entity =>
            {
                entity.ToTable("trickles");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Text).HasColumnName("text");
                entity.Property(e => e.QuestionType).HasColumnName("question_type");
                entity.HasMany(t => t.Answers)
                      .WithOne()
                      .HasForeignKey(a => a.TricklerId)
                      .HasConstraintName("fk_answers_trickles_trickler_id")
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(t => t.Availability)
                      .WithMany()
                      .HasForeignKey(t => t.AvailabilityId)
                      .HasConstraintName("fk_trickles_availabilities_availability_id")
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(t => t.AvailabilityId);
            });

            modelBuilder.Entity<Availability>(entity =>
            {
                entity.ToTable("availabilities");
                entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(e => e.Type).HasColumnName("type");
                entity.Property(e => e.From).HasColumnName("from_date");
                entity.Property(e => e.Until).HasColumnName("until_date");
                entity.Property(e => e.DatesJson).HasColumnName("dates").HasColumnType("jsonb");
                entity.Property(e => e.DaysOfWeekJson).HasColumnName("days_of_week").HasColumnType("jsonb");
            });

            modelBuilder.Entity<Answer>(entity =>
            {
                entity.ToTable("answers");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TricklerId).HasColumnName("trickler_id");
                entity.Property(e => e.AnswerText).HasColumnName("answer").HasColumnType("text");
                entity.HasIndex(a => a.TricklerId);
            });

            modelBuilder.Entity<UserTrickle>(entity =>
            {
                entity.ToTable("user_trickles");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.TrickleId).HasColumnName("trickler_id");
                entity.Property(e => e.AttemptsToday).HasColumnName("attempts_today");
                entity.Property(e => e.AttemptsDate).HasColumnName("attempts_date");
                entity.Property(e => e.AttemptCountTotal).HasColumnName("attempt_count_total");
                entity.Property(e => e.LastAttemptAt).HasColumnName("last_attempt_at");
                entity.Property(e => e.IsSolved).HasColumnName("is_solved");
                entity.Property(e => e.SolvedAt).HasColumnName("solved_at");
                entity.Property(e => e.RewardCode).HasColumnName("reward_code").HasMaxLength(100);
                entity.Property(e => e.RowVersion).HasColumnName("row_version").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
                entity.HasIndex(u => u.UserId);
                entity.HasIndex(u => u.TrickleId);
                entity.HasIndex(u => new { u.UserId, u.TrickleId }).IsUnique();
                entity.HasIndex(u => u.RewardCode).IsUnique();
                entity.HasOne(u => u.Trickle)
                      .WithMany()
                      .HasForeignKey(u => u.TrickleId)
                      .HasConstraintName("fk_user_trickles_trickles_trickler_id")
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ApplicationUser>().ToTable("users");
            modelBuilder.Entity<IdentityRole>().ToTable("roles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("user_roles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("user_claims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("user_logins");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("user_tokens");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("role_claims");
        }
    }
}
