namespace Kursserver.Extracts
{
    public class ExtractUpdateAttendance
    {
        public int UserId { get; set; }
        public string Date { get; set; }

        public bool Attended { get; set; }
    }
}
