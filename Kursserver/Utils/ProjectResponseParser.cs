using Kursserver.Dto;

namespace Kursserver.Services
{
    public static class ProjectResponseParser
    {
        public static ProjectResponse Parse(string response)
        {
            return new ProjectResponse
            {
                Success = true,  // ← Set success
                Title = ExtractSection(response, "TITLE:"),
                Description = ExtractSection(response, "DESCRIPTION:"),
                Difficulty = ParseDifficulty(ExtractSection(response, "DIFFICULTY:")),
                TechStack = ExtractSection(response, "TECH STACK:"),
                LearningGoals = ExtractSection(response, "LEARNING GOALS:"),
                UserStories = ExtractSection(response, "USER STORIES:"),
                DesignSpecs = ExtractSection(response, "DESIGN SPECS:"),
                AssetsNeeded = ExtractSection(response, "ASSETS NEEDED:"),
                StarterHtml = ExtractSection(response, "STARTER HTML:"),
                SolutionHtml = ExtractSection(response, "SOLUTION HTML:"),
                SolutionCss = ExtractSection(response, "SOLUTION CSS:"),
                SolutionJs = ExtractSection(response, "SOLUTION JS:"),
                BonusChallenges = ExtractSection(response, "BONUS CHALLENGES:")
            };
        }

        private static int ParseDifficulty(string difficultyStr)
        {
            if (!string.IsNullOrEmpty(difficultyStr) &&
                int.TryParse(difficultyStr.Split('/')[0].Trim(), out int diff))
            {
                return diff;
            }
            return 1;
        }

        private static string ExtractSection(string response, string header)
        {
            var startIndex = response.IndexOf(header);
            if (startIndex == -1) return "";

            startIndex += header.Length;

            var headers = new[]
            {
                "TITLE:", "DESCRIPTION:", "DIFFICULTY:", "TECH STACK:",
                "LEARNING GOALS:", "USER STORIES:", "DESIGN SPECS:",
                "ASSETS NEEDED:", "STARTER HTML:", "SOLUTION HTML:",
                "SOLUTION CSS:", "SOLUTION JS:", "BONUS CHALLENGES:"
            };

            var endIndex = response.Length;
            foreach (var h in headers)
            {
                if (h == header) continue;
                var idx = response.IndexOf(h, startIndex);
                if (idx != -1 && idx < endIndex)
                {
                    endIndex = idx;
                }
            }

            return response.Substring(startIndex, endIndex - startIndex).Trim();
        }
    }
}