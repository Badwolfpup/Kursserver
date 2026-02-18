namespace Kursserver.Dto
{
    public class AddAvailabilityDto
    {
        public int AdminId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
