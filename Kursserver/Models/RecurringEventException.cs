using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class RecurringEventException
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RecurringEventId { get; set; }
        public RecurringEvent? RecurringEvent { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public bool IsDeleted { get; set; } = false;

        public string? Name { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }
    }
}
