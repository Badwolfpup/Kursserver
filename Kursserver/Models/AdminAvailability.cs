using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class AdminAvailability
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AdminId { get; set; }
        public User? Admin { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public bool IsBooked { get; set; } = false;
    }
}
