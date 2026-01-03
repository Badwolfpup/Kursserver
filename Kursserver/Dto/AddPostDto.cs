using Kursserver.Models;
using System.ComponentModel.DataAnnotations;

namespace Kursserver.Dto
{
    public class AddPostDto
    {
        public int Id { get; set; }

        public string Html { get; set; }

        public string Delta { get; set; }

        public DateTime PublishedAt { get; set; }

        public int? UserId { get; set; }  // FK 

        public User? User { get; set; }

        public bool Pinned { get; set; } = false;
    }
}
