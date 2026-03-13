namespace Kursserver.Dto
{
    public class UpdateRecurringEventDto
    {
        public string? Name { get; set; }
        public DayOfWeek? Weekday { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string? Frequency { get; set; }
        public int? Classroom { get; set; }
    }
}
