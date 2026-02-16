using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public string Type { get; set; } = "bug"; // bug, feature, question, other

        [Required]
        public string Status { get; set; } = "Open"; // Open, InProgress, Closed

        [Required]
        public int SenderId { get; set; }
        public User? Sender { get; set; }

        public int? RecipientId { get; set; }
        public User? Recipient { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
