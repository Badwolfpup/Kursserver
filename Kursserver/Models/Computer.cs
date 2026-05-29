using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class Computer
    {
        [Key]
        public int Id { get; set; }

        // The id number written on the physical machine.
        [Required]
        public int Number { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        // When set, the whole computer is dedicated to one student ("own computer").
        // Null = shared (assigned per day/period via ComputerAssignment).
        public int? OwnerStudentId { get; set; }
        public User? OwnerStudent { get; set; }

        // Only meaningful when OwnerStudentId is set: the student takes it home.
        [Required]
        public bool TakesHome { get; set; } = false;
    }
}
