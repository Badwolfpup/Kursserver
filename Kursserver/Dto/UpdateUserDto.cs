using Kursserver.Models;
using System.ComponentModel.DataAnnotations;

namespace Kursserver.Dto
{
    public class UpdateUserDto
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public Role? AuthLevel { get; set; }
        public bool? IsActive { get; set; } = true;

        public int? Course { get; set; } = 3;

        public int? CoachId { get; set; }
    }
}
