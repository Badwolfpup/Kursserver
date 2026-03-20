namespace Kursserver.Dto
{
    public class AssignSeatDto
    {
        public int ClassroomId { get; set; }
        public int DayOfWeek { get; set; }
        public string Period { get; set; } = "";
        public int Row { get; set; }
        public int Column { get; set; }
        public int StudentId { get; set; }
    }
}
