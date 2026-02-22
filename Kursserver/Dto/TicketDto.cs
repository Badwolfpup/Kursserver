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

    public class AddTicketTimeSuggestionDto
    {
        public int TicketId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class RespondToTimeSuggestionDto
    {
        public bool Accept { get; set; }
        public string? DeclineReason { get; set; }
    }
}
