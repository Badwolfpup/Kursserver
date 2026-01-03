using Kursserver.Models;

namespace Kursserver.Dto
{
    public class UpdatePostDto
    {
        public int Id { get; set; }

        public string? Html { get; set; }

        public string? Delta { get; set; }

        public bool? Pinned { get; set; } = false;
    }
}
