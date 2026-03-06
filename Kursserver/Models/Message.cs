namespace Kursserver.Models;

public class Message
{
    public int Id { get; set; }
    public int ThreadId { get; set; }
    public Thread? Thread { get; set; }
    public int SenderId { get; set; }
    public User? Sender { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
