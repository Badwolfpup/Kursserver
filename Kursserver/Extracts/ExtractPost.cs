namespace Kursserver.Extracts
{
    public class ExtractPost
    {
        public string Email { get; set; }
        public string Html { get; set; }
        public List<object> Delta { get; set; }
    }
}
