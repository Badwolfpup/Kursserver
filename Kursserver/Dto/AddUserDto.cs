namespace Kursserver.Dto
{
    public class AddUserDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int AuthLevel { get; set; }
        public int Course { get; set; }
    }
}
