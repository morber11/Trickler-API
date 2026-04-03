using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Trickler_API.Models
{
    public class UserDetails
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("user_id", TypeName = "text")]
        public string UserId { get; set; } = string.Empty;


        [Column("total_score", TypeName = "integer")]
        public int TotalScore { get; set; }

        [Column("is_private", TypeName = "boolean")]
        public bool IsPrivate { get; set; } = false;
    }
}
