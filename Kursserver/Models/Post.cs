using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Html { get; set; }  

        [Required]
        public string Delta { get; set; } 

        [Required]
        public DateTime PublishedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        public string Author { get; set; }

        public int? UserId { get; set; }  // FK 

        public User? User { get; set; }

        public bool Pinned { get; set; } = false;
    }

}
