using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required] 
        public string Description { get; set; }

        [Required]
        public string Html { get; set; }

        [Required]
        public string Css { get; set; }

        [Required]
        public string Javascript { get; set; }

        [Required]
        public int Difficulty { get; set; } = 1;
        public List<string> Tags { get; set; }

        public string? ImageUrl { get; set; }

    }
}
