namespace Kursserver.Models
{
    public class ProjectHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TechStack { get; set; } = "";
        public int Difficulty { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string? LearningGoals { get; set; }
        public string? UserStories { get; set; }
        public string? DesignSpecs { get; set; }
        public string? AssetsNeeded { get; set; }
        public string? StarterHtml { get; set; }
        public string? BonusChallenges { get; set; }
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
