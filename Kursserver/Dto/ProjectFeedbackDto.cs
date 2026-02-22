namespace Kursserver.Dto
{
    public class ProjectFeedbackDto
    {
        public string TechStack { get; set; } = "";
        public int Difficulty { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string? SolutionHtml { get; set; }
        public string? SolutionCss { get; set; }
        public string? SolutionJs { get; set; }
        public bool IsCompleted { get; set; }
        public bool? IsPositive { get; set; }
        public string? FeedbackReason { get; set; }
        public string? FeedbackComment { get; set; }
    }
}
