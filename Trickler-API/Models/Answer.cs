using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Trickler_API.Models
{
    public class Answer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("trickler_id", TypeName = "integer")]
        public int TricklerId { get; set; }

        private string _answerText = string.Empty;

        [Column("answer", TypeName = "text")]
        public string AnswerText
        {
            get => _answerText;
            set
            {
                _answerText = value?.Trim() ?? string.Empty;
                NormalizedAnswer = _answerText.ToLowerInvariant();
            }
        }

        [Column("normalized_answer", TypeName = "text")]
        public string NormalizedAnswer { get; set; } = string.Empty;
    }
}
