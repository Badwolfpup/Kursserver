using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class Exercise
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Javascript { get; set; }

        [Required]
        public string ExpectedResult { get; set; }

        [Required]
        public int Difficulty { get; set; } = 1;

        [Required]
        public string ExerciseType { get; set; }
        public List<string> Clues { get; set; }

        public string GoodToKnow { get; set; }

    }
}
