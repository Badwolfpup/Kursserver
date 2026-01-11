namespace Kursserver.Dto
{
    public class PostDto
    {
        public string Email { get; set; }
        public string Html { get; set; }
        public string Delta { get; set; }

        public int UserId { get; set; }

        public bool Pinned { get; set; } = false;
    }
}
