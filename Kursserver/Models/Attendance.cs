using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }  // FK

        public User User { get; set; }

        [Required]
        public DateTime Date { get; set; }

    }
}
