namespace Kursserver.Dto
{
    public class UpdateBookingStatusDto
    {
        public string Status { get; set; } = "";
        public string? Reason { get; set; }
    }
}
