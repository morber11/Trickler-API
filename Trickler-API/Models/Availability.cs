using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trickler_API.Models
{
    public class Availability
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("type", TypeName = "integer")]
        public AvailabilityType Type { get; set; }

        [Column("from_date", TypeName = "date")]
        public DateOnly? From { get; set; }

        [Column("until_date", TypeName = "date")]
        public DateOnly? Until { get; set; }

        [Column("dates", TypeName = "jsonb")]
        [JsonIgnore]
        public string? DatesJson { get; set; }

        [Column("days_of_week", TypeName = "jsonb")]
        [JsonIgnore]
        public string? DaysOfWeekJson { get; set; }

        [NotMapped]
        public List<DateOnly>? Dates
        {
            get
            {
                if (string.IsNullOrEmpty(DatesJson))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<List<DateOnly>>(DatesJson);
            }
            set
            {
                DatesJson = value is not null ? JsonSerializer.Serialize(value) : null;
            }
        }

        [NotMapped]
        public List<string>? DaysOfWeek
        {
            get
            {
                if (string.IsNullOrEmpty(DaysOfWeekJson))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<List<string>>(DaysOfWeekJson);
            }
            set
            {
                DaysOfWeekJson = value is not null ? JsonSerializer.Serialize(value) : null;
            }
        }
    }
}
