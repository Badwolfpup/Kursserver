namespace Kursserver.Dto
{
    public class CreateBookingDto
    {
        public int? AdminAvailabilityId { get; set; }
        public int? AdminId { get; set; }
        public int? CoachId { get; set; }
        public int? StudentId { get; set; }
        public string Note { get; set; } = "";
        public string MeetingType { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Force { get; set; } = false;
    }
}
