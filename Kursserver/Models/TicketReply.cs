using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class TicketReply
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        [Required]
        public int SenderId { get; set; }
        public User? Sender { get; set; }

        [Required]
        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
