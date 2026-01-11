namespace Kursserver.Dto
{
    public class UpdatePermissionDto
    {
        public int UserId { get; set; }

        public bool? Html { get; set; }
        public bool? Css { get; set; }
        public bool? Javascript { get; set; }
        public bool? Variable { get; set; }
        public bool? Conditionals { get; set; }
        public bool? Loops { get; set; }
        public bool? Functions { get; set; }
        public bool? Arrays { get; set; }
        public bool? Objects { get; set; }
    }
}
