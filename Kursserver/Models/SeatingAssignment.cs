using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class SeatingAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClassroomId { get; set; }

        [Required]
        public int DayOfWeek { get; set; }

        [Required]
        [MaxLength(2)]
        public string Period { get; set; } = "";

        [Required]
        public int Row { get; set; }

        [Required]
        public int Column { get; set; }

        [Required]
        public int StudentId { get; set; }
        public User? Student { get; set; }
    }
}
