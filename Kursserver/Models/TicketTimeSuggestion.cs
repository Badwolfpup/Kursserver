using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class TicketTimeSuggestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        [Required]
        public int SuggestedById { get; set; }
        public User? SuggestedBy { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        public string Status { get; set; } = "pending"; // pending, accepted, declined

        public string? DeclineReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
