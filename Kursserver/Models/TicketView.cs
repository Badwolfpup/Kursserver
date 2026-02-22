using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class TicketView
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public DateTime LastViewedAt { get; set; } = DateTime.Now;
    }
}
