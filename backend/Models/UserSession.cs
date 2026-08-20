namespace backend.Models;

public class UserSession
{
    public int Id { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime ConnectedAt { get; set; }

    public DateTime LastActivityAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? LoggedOutAt { get; set; }
}