namespace Kursserver.Dto
{
    public record ExerciseRequest
    {
        public string Topic { get; init; } = string.Empty;
        public string Language { get; init; } = "JavaScript";
        public int Difficulty { get; init; } = 1;

        // Valid topics for validation
        public static readonly string[] ValidTopics =
        {
        "Variables and DataTypes",
        "Operators",
        "Conditionals",
        "Loops",
        "Functions",
        "Arrays",
        "Objects",
        "Strings",
        "DOM Basics",
        "Events"
    };

        // Valid languages
        public static readonly string[] ValidLanguages =
        {
        "JavaScript",
        "Python",
        "C#",
        "Java",
        "TypeScript"
    };

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(Topic))
            {
                error = "Topic is required";
                return false;
            }

            if (Difficulty < 1 || Difficulty > 5)
            {
                error = "Difficulty level must be between 1 and 5";
                return false;
            }

            // DOM and Events only for JavaScript
            if (Topic.ToLower() is "dom basics" or "dom" or "events")
            {
                if (Language.ToLower() is not "javascript" and not "js")
                {
                    error = $"'{Topic}' is only available for JavaScript";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }


}
