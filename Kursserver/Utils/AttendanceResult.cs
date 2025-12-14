namespace Kursserver.Utils
{
    public class AttendanceResult
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int UserId { get; set; }

        public DateTime Date { get; set; }
        public int Status { get; set; }
    }
}
