using Kursserver.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace Kursserver.Dto
{
    public class FetchUsersDto
    {
        public Role? AuthLevel { get; set; }
    }
}
