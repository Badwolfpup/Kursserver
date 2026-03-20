namespace Kursserver.Dto
{
    public class UpdateBusyTimeDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Note { get; set; }
    }
}
