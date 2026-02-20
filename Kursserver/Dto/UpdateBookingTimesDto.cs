namespace Kursserver.Dto
{
    public class UpdateBookingTimesDto
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Reason { get; set; }
    }
}
