namespace Kursserver.Dto
{
    public class BookAvailabilityDto
    {
        public int? AdminAvailabilityId { get; set; }
        public int CoachId { get; set; }
        public int? StudentId { get; set; }
        public string Note { get; set; } = "";
        public string MeetingType { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
