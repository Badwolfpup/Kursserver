namespace Kursserver.Models;

public class ThreadView
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int ThreadId { get; set; }
    public Thread? Thread { get; set; }
    public DateTime LastViewedAt { get; set; } = DateTime.UtcNow;
}
