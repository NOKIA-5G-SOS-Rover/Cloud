using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class SessionService
{
    private readonly AppDbContext _context;

    public SessionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserSession> CreateSessionAsync(User user)
    {
        var now = DateTime.UtcNow;

        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = user.Id,
            ConnectedAt = now,
            LastActivityAt = now,
            ExpiresAt = now.AddHours(8)
        };

        _context.UserSessions.Add(session);

        await _context.SaveChangesAsync();

        return session;
    }

    public async Task<UserSession?> GetValidSessionAsync(
        string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return null;

        return await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s =>
                s.SessionId == sessionId &&
                s.LoggedOutAt == null &&
                s.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<UserSession?> RevokeSessionAsync(
        int sessionId)
    {
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                s.LoggedOutAt == null &&
                s.ExpiresAt > DateTime.UtcNow);

        if (session == null)
            return null;

        session.LoggedOutAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return session;
    }

    public async Task<int> RevokeAllSessionsForUserAsync(
        int userId)
    {
        var sessions = await _context.UserSessions
            .Where(s =>
                s.UserId == userId &&
                s.LoggedOutAt == null &&
                s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        if (sessions.Count == 0)
            return 0;

        var now = DateTime.UtcNow;

        foreach (var session in sessions)
        {
            session.LoggedOutAt = now;
        }

        await _context.SaveChangesAsync();

        return sessions.Count;
    }

    public async Task UpdateActivityAsync(
        UserSession session)
    {
        if (session.LoggedOutAt != null)
            return;

        if (session.ExpiresAt <= DateTime.UtcNow)
            return;

        session.LastActivityAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> LogoutAsync(
        string sessionId)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s =>
                s.SessionId == sessionId &&
                s.LoggedOutAt == null);

        if (session == null)
            return false;

        session.LoggedOutAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public bool IsOnline(
        UserSession session)
    {
        return
            session.LoggedOutAt == null &&
            session.ExpiresAt > DateTime.UtcNow &&
            session.LastActivityAt >
                DateTime.UtcNow.AddMinutes(-2);
    }

    public async Task<int> CleanupExpiredSessionsAsync()
    {
        var expiredSessions =
            await _context.UserSessions
                .Where(s =>
                    s.LoggedOutAt == null &&
                    s.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

        if (expiredSessions.Count == 0)
            return 0;

        var now = DateTime.UtcNow;

        foreach (var session in expiredSessions)
        {
            session.LoggedOutAt = now;
        }

        await _context.SaveChangesAsync();

        return expiredSessions.Count;
    }
}