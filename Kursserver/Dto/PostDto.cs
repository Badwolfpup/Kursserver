namespace Kursserver.Dto
{
    public class PostDto
    {
        public string Email { get; set; }
        public string Html { get; set; }

        public int UserId { get; set; } 
        public List<object> Delta { get; set; }
    }
}
