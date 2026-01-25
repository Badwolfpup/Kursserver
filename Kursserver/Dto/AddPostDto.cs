namespace Kursserver.Dto
{
    public class AddPostDto
    {
        public int? UserId { get; set; }  // FK 

        public string Html { get; set; }

        public string Delta { get; set; }

        public DateTime? PublishDate { get; set; }

        public bool Pinned { get; set; } = false;
    }
}
