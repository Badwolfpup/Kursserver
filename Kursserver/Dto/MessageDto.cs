namespace Kursserver.Dto;

public class SendMessageDto
{
    public int RecipientId { get; set; }
    public string Content { get; set; } = "";
    public int? StudentContextId { get; set; }
}

public class AddBugReportDto
{
    public string Type { get; set; } = "bug";
    public string Content { get; set; } = "";
}
