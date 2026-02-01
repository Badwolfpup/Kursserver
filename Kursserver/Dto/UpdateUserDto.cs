using Kursserver.Models;

namespace Kursserver.Dto
{
    public class UpdateUserDto
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }

        public string? Telephone { get; set; }

        public Role? AuthLevel { get; set; }
        public bool? IsActive { get; set; }

        public int? Course { get; set; }

        public int? CoachId { get; set; }

        public int? ContactId { get; set; }


        public bool? ScheduledMonAm { get; set; }
        public bool? ScheduledMonPm { get; set; }
        public bool? ScheduledTueAm { get; set; }
        public bool? ScheduledTuePm { get; set; }
        public bool? ScheduledWedAm { get; set; }
        public bool? ScheduledWedPm { get; set; }
        public bool? ScheduledThuAm { get; set; }
        public bool? ScheduledThuPm { get; set; }
    }
}
