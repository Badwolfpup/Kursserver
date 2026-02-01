using System.ComponentModel.DataAnnotations;


namespace Kursserver.Models
{
    public enum Role
    {
        Admin = 1,
        Teacher = 2,
        Coach = 3,
        Student = 4,
        Guest = 5
    }

    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string? Telephone { get; set; }
        [Required]
        public Role AuthLevel { get; set; }
        [Required]
        public bool IsActive { get; set; } = true;

        public int? Course { get; set; } = 3;

        public int? CoachId { get; set; }
        public User? Coach { get; set; }

        public int? ContactId { get; set; }
        public User? Contact { get; set; }

        public bool ScheduledMonAm { get; set; } = true;
        public bool ScheduledMonPm { get; set; } = true;

        public bool ScheduledTueAm { get; set; } = true;
        public bool ScheduledTuePm { get; set; } = true;

        public bool ScheduledWedAm { get; set; } = true;
        public bool ScheduledWedPm { get; set; } = true;

        public bool ScheduledThuAm { get; set; } = true;
        public bool ScheduledThuPm { get; set; } = true;
    }
}
