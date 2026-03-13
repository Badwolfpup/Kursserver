namespace Kursserver.Dto
{
    public class CreateRecurringEventDto
    {
        public string Name { get; set; } = "";
        public DayOfWeek Weekday { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Frequency { get; set; } = "weekly";
        public DateTime StartDate { get; set; }
        public int? AdminId { get; set; }
        public int? Classroom { get; set; }
    }
}
