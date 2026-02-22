namespace Kursserver.Models
{
    public class ProjectHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TechStack { get; set; } = "";
        public int Difficulty { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = ""; // Store first 200 chars
        public DateTime CreatedAt { get; set; }

        public bool? IsPositive { get; set; }
        public string? FeedbackReason { get; set; }
        public string? FeedbackComment { get; set; }
        public bool IsCompleted { get; set; }
        public string? SolutionHtml { get; set; }
        public string? SolutionCss { get; set; }
        public string? SolutionJs { get; set; }

        public User User { get; set; } = null!;
    }
}
