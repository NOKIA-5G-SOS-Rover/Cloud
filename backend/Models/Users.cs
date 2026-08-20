using backend.Constants;

namespace backend.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = Roles.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserPermission> Permissions { get; set; }
        = new List<UserPermission>();

    public ICollection<UserSession> Sessions { get; set; }
        = new List<UserSession>();
}