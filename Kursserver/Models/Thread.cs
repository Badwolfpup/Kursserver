namespace Kursserver.Models;

public class Thread
{
    public int Id { get; set; }
    public int User1Id { get; set; }
    public User? User1 { get; set; }
    public int User2Id { get; set; }
    public User? User2 { get; set; }
    public int? StudentContextId { get; set; }
    public User? StudentContext { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
