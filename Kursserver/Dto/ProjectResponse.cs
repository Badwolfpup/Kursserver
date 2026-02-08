namespace Kursserver.Dto
{
    public class ProjectResponse
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int Difficulty { get; set; }
        public string TechStack { get; set; } = "";
        public string LearningGoals { get; set; } = "";
        public string UserStories { get; set; } = "";
        public string DesignSpecs { get; set; } = "";
        public string AssetsNeeded { get; set; } = "";
        public string StarterHtml { get; set; } = "";
        public string SolutionHtml { get; set; } = "";
        public string SolutionCss { get; set; } = "";
        public string SolutionJs { get; set; } = "";
        public string BonusChallenges { get; set; } = "";
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
