namespace Kursserver.Dto
{
    public class ExerciseFeedbackDto
    {
        public string Topic { get; set; } = "";
        public string Language { get; set; } = "";
        public int Difficulty { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string? Solution { get; set; }
        public string? Asserts { get; set; }
        public bool IsCompleted { get; set; }
        public bool? IsPositive { get; set; }
        public string? FeedbackReason { get; set; }
        public string? FeedbackComment { get; set; }
    }
}
