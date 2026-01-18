namespace Kursserver.Dto
{
    public class AddExerciseDto
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string Javascript { get; set; }

        public int Difficulty { get; set; }

        public string ExpectedResult { get; set; }

        public List<string>? Tags { get; set; } = new List<string>();

        public List<string>? Clues { get; set; } = new List<string>();


    }
}
