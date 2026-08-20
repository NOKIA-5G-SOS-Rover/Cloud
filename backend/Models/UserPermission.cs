namespace backend.Models;

public class UserPermission
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string Permission { get; set; } = string.Empty;

    public int GrantedByAdminId { get; set; }

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}