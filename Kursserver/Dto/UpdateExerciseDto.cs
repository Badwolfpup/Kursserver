namespace Kursserver.Dto
{
    public class UpdateExerciseDto
    {
        public int Id { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? Javascript { get; set; }

        public string? ExpectedResult { get; set; }


        public List<string>? Clues { get; set; }

        public int? Difficulty { get; set; }

        public string? ExerciseType { get; set; }

        public string? GoodToKnow { get; set; }

    }
}
