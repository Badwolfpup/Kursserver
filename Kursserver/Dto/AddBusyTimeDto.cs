namespace Kursserver.Dto
{
    public class AddBusyTimeDto
    {
        public int AdminId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Note { get; set; }
        public bool Force { get; set; } = false;
    }
}
