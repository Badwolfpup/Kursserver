using Kursserver.Models;

namespace Kursserver.Dto
{
    public class UpdateBookingStatusDto
    {
        public BookingStatus Status { get; set; }
        public string? Reason { get; set; }
    }
}
