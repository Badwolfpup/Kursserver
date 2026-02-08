namespace Kursserver.Models
{
    public class ExerciseHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Topic { get; set; } = "";
        public string Language { get; set; } = "";
        public int Difficulty { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = ""; // Store first 200 chars
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
