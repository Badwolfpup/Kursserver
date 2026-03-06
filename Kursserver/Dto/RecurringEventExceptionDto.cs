namespace Kursserver.Dto
{
    public class RecurringEventExceptionDto
    {
        public bool IsDeleted { get; set; } = false;
        public string? Name { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
    }
}
