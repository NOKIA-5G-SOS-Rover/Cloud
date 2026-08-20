using backend.Data;
using backend.Models;

namespace backend.Services;

public class AuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        int? userId,
        string username,
        string action,
        string details)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Username = string.IsNullOrWhiteSpace(username)
                ? "Unknown"
                : username.Trim(),

            Action = string.IsNullOrWhiteSpace(action)
                ? "UNKNOWN_ACTION"
                : action.Trim().ToUpperInvariant(),

            Details = details?.Trim() ?? string.Empty,

            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);

        await _context.SaveChangesAsync();
    }
}