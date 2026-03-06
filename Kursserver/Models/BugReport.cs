namespace Kursserver.Models;

public class BugReport
{
    public int Id { get; set; }
    public string Type { get; set; } = "bug";
    public string Content { get; set; } = "";
    public int SenderId { get; set; }
    public User? Sender { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
