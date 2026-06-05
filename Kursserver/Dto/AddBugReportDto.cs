namespace Kursserver.Dto;

public class AddBugReportDto
{
    public string Type { get; set; } = "bug";
    public string Content { get; set; } = "";
}
