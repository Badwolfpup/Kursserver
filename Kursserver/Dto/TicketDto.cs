namespace Kursserver.Dto
{
    public class AddTicketDto
    {
        public string Subject { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } = "bug";
        public int? RecipientId { get; set; }
    }

    public class UpdateTicketDto
    {
        public int Id { get; set; }
        public string? Status { get; set; }
        public int? RecipientId { get; set; }
    }

    public class AddTicketReplyDto
    {
        public int TicketId { get; set; }
        public string Message { get; set; }
    }
}
