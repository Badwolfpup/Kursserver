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

        public string ProjectType { get; set; }
    }
}
