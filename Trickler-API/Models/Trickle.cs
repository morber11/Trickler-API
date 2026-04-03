using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Trickler_API.Models
{
    public class Trickle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(1000)]
        [Column(TypeName = "varchar(1000)")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Column(TypeName = "varchar(1000)")]
        public string Text { get; set; } = string.Empty;

        [Required]
        [Column("question_type", TypeName = "integer")]
        public QuestionType QuestionType { get; set; } = QuestionType.Single;
        public List<Answer> Answers { get; set; } = [];

        [Column("score", TypeName = "integer")]
        public int Score { get; set; } = 0;

        [Column("reward_text", TypeName = "text")]
        public string RewardText { get; set; } = string.Empty;

        [Column("availability_id")]
        public int? AvailabilityId { get; set; }
        public Availability? Availability { get; set; }

        [Column("attempts_per_trickle", TypeName = "integer")]
        public int AttemptsPerTrickle { get; set; } = -1;
    }
}
