namespace Kursserver.Dto
{
    public record ExerciseResponse
    {
        public bool Success { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }

        public string? Example { get; init; }

        public string? Assumptions { get; init; }

        public string? FunctionSignature { get; init; }
        public List<AssertItem>? Asserts { get; init; } = new List<AssertItem>();
        public string? Solution { get; init; }


        public string? Error { get; init; }
    }

    public record AssertItem
    {
        public string Comment { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
    }
}
