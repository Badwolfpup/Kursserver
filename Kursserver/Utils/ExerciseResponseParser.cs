using Kursserver.Dto;

namespace Kursserver.Utils
{
    public static class ExerciseResponseParser
    {
        public static (
            string Title,
            string Description,
            string Example,
            string Assumptions,
            string FunctionSignature,
            List<AssertItem> Asserts,
            string Solution
        ) ParseAssertResponse(string rawResponse)
        {
            var title = ExtractSection(rawResponse, "TITLE:", "DESCRIPTION:");
            var description = ExtractSection(rawResponse, "DESCRIPTION:", "EXAMPLE:");
            var example = ExtractSection(rawResponse, "EXAMPLE:", "ASSUMPTIONS:");
            var assumptions = ExtractSection(rawResponse, "ASSUMPTIONS:", "FUNCTION SIGNATURE:");
            var functionSignature = ExtractSection(rawResponse, "FUNCTION SIGNATURE:", "SOLUTION:");
            var solution = ExtractSection(rawResponse, "SOLUTION:", "ASSERTS:");
            var assertsRaw = ExtractSection(rawResponse, "ASSERTS:", null);
            var asserts = ParseAsserts(assertsRaw);

            return (
                Title: title.Trim(),
                Description: description.Trim(),
                Example: example.Trim(),
                Assumptions: assumptions.Trim(),
                FunctionSignature: functionSignature.Trim(),
                Asserts: asserts,
                Solution: solution.Trim()
            );
        }

        private static List<AssertItem> ParseAsserts(string assertsRaw)
        {
            var result = new List<AssertItem>();
            var lines = assertsRaw.Split('\n');

            string? currentComment = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // Is this a comment line?
                if (trimmed.StartsWith("//") || trimmed.StartsWith("#"))
                {
                    currentComment = trimmed;
                }
                // Is this code?
                else if (IsAssertLine(trimmed))
                {
                    result.Add(new AssertItem
                    {
                        Comment = currentComment ?? string.Empty,
                        Code = trimmed
                    });
                    currentComment = null;
                }
            }

            return result;
        }

        private static bool IsAssertLine(string line)
        {
            var lower = line.ToLower();
            return lower.StartsWith("assert") ||
                   lower.StartsWith("expect") ||
                   lower.StartsWith("assert.") ||
                   lower.StartsWith("expect(");
        }

        private static string ExtractSection(string text, string startMarker, string? endMarker)
        {
            var startIndex = text.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1) return string.Empty;

            startIndex += startMarker.Length;

            int endIndex;
            if (endMarker != null)
            {
                endIndex = text.IndexOf(endMarker, startIndex, StringComparison.OrdinalIgnoreCase);
                if (endIndex == -1) endIndex = text.Length;
            }
            else
            {
                endIndex = text.Length;
            }

            return text.Substring(startIndex, endIndex - startIndex);
        }
    }
}