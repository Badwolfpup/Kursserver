using System.ComponentModel.DataAnnotations;

namespace Kursserver.Models
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }

        public bool Html { get; set; } = false;
        public bool Css { get; set; } = false;
        public bool Javascript { get; set; } = false;
        public bool Variable { get; set; } = false;
        public bool Conditionals { get; set; } = false;
        public bool Loops { get; set; } = false;
        public bool Functions { get; set; } = false;
        public bool Arrays { get; set; } = false;
        public bool Objects { get; set; } = false;
    }
}
