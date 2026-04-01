using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Trickler_API.Models
{
    public class UserTrickle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("user_id", TypeName = "text")]
        public string UserId { get; set; } = string.Empty;

        [Column("trickler_id", TypeName = "integer")]
        public int TrickleId { get; set; }
        public Trickle? Trickle { get; set; }

        [Column("attempts_today", TypeName = "integer")]
        public int AttemptsToday { get; set; }

        [Column("attempts_date", TypeName = "timestamp with time zone")]
        public DateTime AttemptsDate { get; set; }

        [Column("attempt_count_total", TypeName = "integer")]
        public int AttemptCountTotal { get; set; }

        [Column("last_attempt_at", TypeName = "timestamp with time zone")]
        public DateTime? LastAttemptAt { get; set; }

        [Column("is_solved", TypeName = "boolean")]
        public bool IsSolved { get; set; }

        [Column("solved_at", TypeName = "timestamp with time zone")]
        public DateTime? SolvedAt { get; set; }

        [MaxLength(100)]
        [Column("reward_code", TypeName = "character varying(100)")]
        public string? RewardCode { get; set; }
    }
}