using Kursserver.Models;

namespace Kursserver.Dto
{
    public class AddUserDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public Role AuthLevel { get; set; }
        public int? Course { get; set; }

        public int? CoachId { get; set;  }
    }
}
