namespace backend.Models;

public class RoverControlSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime LastActivityAt { get; set; }

    public DateTime? EndedAt { get; set; }
}