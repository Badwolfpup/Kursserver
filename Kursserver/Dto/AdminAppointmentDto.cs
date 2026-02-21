namespace Kursserver.Dto
{
    public class AdminAppointmentDto
    {
        public int CoachId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Note { get; set; } = "";
        public string MeetingType { get; set; } = "";
        public bool Force { get; set; } = false;
    }
}
