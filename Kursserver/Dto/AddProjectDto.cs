using System.ComponentModel.DataAnnotations;

namespace Kursserver.Dto
{
    public class AddProjectDto
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string Html { get; set; }

        public string Css { get; set; }

        public string Javascript { get; set; }

        public int Difficulty { get; set; }

        public List<string>? Tags { get; set; } = new List<string>();

        public string? ImageUrl { get; set; }
    }
}
