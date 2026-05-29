namespace Kursserver.Dto
{
    public class AddComputerDto
    {
        public int Number { get; set; }
    }

    public class SetComputerOwnerDto
    {
        public int ComputerId { get; set; }
        // null clears the owner (computer becomes shared again).
        public int? StudentId { get; set; }
        public bool TakesHome { get; set; }
    }

    public class AssignComputerDto
    {
        public int ComputerId { get; set; }
        public int DayOfWeek { get; set; }
        public string Period { get; set; } = "";
        public int StudentId { get; set; }
    }
}
