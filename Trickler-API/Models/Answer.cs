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

        [Column("answer", TypeName = "text")]
        public string AnswerText { get; set; } = string.Empty;
    }
}
