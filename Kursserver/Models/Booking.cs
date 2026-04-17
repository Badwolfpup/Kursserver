using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AdminId { get; set; }
        public User? Admin { get; set; }

        public int? CoachId { get; set; }
        public User? Coach { get; set; }

        public int? StudentId { get; set; }
        public User? Student { get; set; }

        public string Note { get; set; } = "";

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public DateTime BookedAt { get; set; } = DateTime.Now;

        public bool Seen { get; set; } = false;

        [Required]
        public MeetingType MeetingType { get; set; }

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public string Reason { get; set; } = "";

        public BookingActor? RescheduledBy { get; set; }

        [Required]
        public BookingActor CreatedByRole { get; set; }
    }
}
