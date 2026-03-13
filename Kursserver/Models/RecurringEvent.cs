using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class RecurringEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public DayOfWeek Weekday { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public string Frequency { get; set; } = "weekly"; // "weekly" or "biweekly"

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public int AdminId { get; set; }
        public User? Admin { get; set; }

        public int? Classroom { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
