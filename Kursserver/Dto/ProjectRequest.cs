namespace Kursserver.Dto
{
    public class ProjectRequest
    {
        public string TechStack { get; set; } = "";
        public int Difficulty { get; set; } = 1;

        private static readonly HashSet<string> ValidProjectTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "html",
            "html-css",
            "html+css",
            "html-css-js",
            "html+css+js",
            "html-css-javascript",
            "html+css+javascript"
        };


        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(TechStack))
            {
                error = "ProjectType is required";
                return false;
            }

            if (!ValidProjectTypes.Contains(TechStack))
            {
                error = $"Invalid ProjectType: '{TechStack}'";
                return false;
            }

            if (Difficulty < 1 || Difficulty > 5)
            {
                error = "Difficulty must be between 1 and 5";
                return false;
            }

            error = "";
            return true;
        }

        public string GetNormalizedProjectType()
        {
            return TechStack.ToLower() switch
            {
                "html" => "html",
                "html-css" or "html+css" => "html+css",
                "html-css-js" or "html+css+js" => "html+css+js",
                _ => TechStack.ToLower()
            };
        }
    }
}