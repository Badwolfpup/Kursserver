using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    // A shared computer used by a given student on a given day/period.
    public class ComputerAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ComputerId { get; set; }
        public Computer? Computer { get; set; }

        [Required]
        public int DayOfWeek { get; set; }

        [Required]
        [MaxLength(2)]
        public string Period { get; set; } = "";

        [Required]
        public int StudentId { get; set; }
        public User? Student { get; set; }
    }
}
