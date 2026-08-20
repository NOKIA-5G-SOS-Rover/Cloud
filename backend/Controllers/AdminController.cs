using backend.Constants;
using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PermissionService _permissionService;
    private readonly SessionService _sessionService;
    private readonly AuditService _auditService;

    public AdminController(
        AppDbContext context,
        PermissionService permissionService,
        SessionService sessionService,
        AuditService auditService)
    {
        _context = context;
        _permissionService = permissionService;
        _sessionService = sessionService;
        _auditService = auditService;
    }

    private IActionResult? CheckAdmin()
    {
        var user = HttpContext.GetCurrentUser();

        if (user == null)
            return Unauthorized(new
            {
                message = "You are not logged in."
            });

        if (user.Role != Roles.Admin)
            return StatusCode(403, new
            {
                message = "Admin access required."
            });

        return null;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var error = CheckAdmin();

        if (error != null)
            return error;

        var users = await _context.Users
            .Include(u => u.Permissions)
            .Include(u => u.Sessions)
            .ToListAsync();

        var now = DateTime.UtcNow;

        var result = users.Select(user =>
        {
            var session = user.Sessions
                .Where(s =>
                    s.LoggedOutAt == null &&
                    s.ExpiresAt > now)
                .OrderByDescending(s => s.ConnectedAt)
                .FirstOrDefault();

            var isOnline =
                session != null &&
                _sessionService.IsOnline(session);

            long? connectedForSeconds = null;

            if (session != null)
            {
                connectedForSeconds =
                    (long)(
                        now -
                        session.ConnectedAt
                    ).TotalSeconds;
            }

            return new
            {
                id = user.Id,
                username = user.Username,
                role = user.Role,

                permissions =
                    user.Role == Roles.Admin
                        ? Permissions.All
                        : user.Permissions
                            .Select(p => p.Permission)
                            .ToArray(),

                isOnline,

                connectedAt =
                    session?.ConnectedAt,

                connectedForSeconds,

                lastActivityAt =
                    session?.LastActivityAt
            };
        });

        return Ok(result);
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetActiveSessions()
    {
        var error = CheckAdmin();

        if (error != null)
            return error;

        var now = DateTime.UtcNow;

        var sessions = await _context.UserSessions
            .Include(s => s.User)
            .Where(s =>
                s.LoggedOutAt == null &&
                s.ExpiresAt > now)
            .OrderByDescending(s => s.ConnectedAt)
            .ToListAsync();

        var result = sessions.Select(session =>
        {
            var connectedFor =
                now - session.ConnectedAt;

            return new
            {
                sessionId = session.Id,
                userId = session.UserId,
                username = session.User.Username,
                role = session.User.Role,

                connectedAt =
                    session.ConnectedAt,

                connectedForSeconds =
                    (long)connectedFor.TotalSeconds,

                lastActivityAt =
                    session.LastActivityAt,

                expiresAt =
                    session.ExpiresAt,

                isOnline =
                    _sessionService.IsOnline(session)
            };
        });

        return Ok(result);
    }

    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> RevokeSession(
        int sessionId)
    {
        var error = CheckAdmin();

        if (error != null)
            return error;

        var admin =
            HttpContext.GetCurrentUser()!;

        var session =
            await _sessionService
                .RevokeSessionAsync(sessionId);

        if (session == null)
        {
            return NotFound(new
            {
                message = "Active session not found."
            });
        }

        await _auditService.LogAsync(
            admin.Id,
            admin.Username,
            "FORCE_LOGOUT",
            $"Forced logout for user {session.User.Username}."
        );

        return Ok(new
        {
            message =
                "Session revoked successfully.",

            user =
                session.User.Username
        });
    }

    [HttpDelete("users/{userId}/sessions")]
    public async Task<IActionResult>
        RevokeAllUserSessions(int userId)
    {
        var error = CheckAdmin();

        if (error != null)
            return error;

        var admin =
            HttpContext.GetCurrentUser()!;

        var targetUser =
            await _context.Users
                .FindAsync(userId);

        if (targetUser == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        var revoked =
            await _sessionService
                .RevokeAllSessionsForUserAsync(userId);

        await _auditService.LogAsync(
            admin.Id,
            admin.Username,
            "FORCE_LOGOUT_ALL",
            $"Revoked {revoked} sessions for user {targetUser.Username}."
        );

        return Ok(new
        {
            message =
                "User sessions revoked.",

            user =
                targetUser.Username,

            revokedSessions =
                revoked
        });
    }

    [HttpGet("users/{userId}/permissions")]
    public async Task<IActionResult> GetPermissions(
        int userId)
    {
        var error = CheckAdmin();

        if (error != null)
            return error;

        var user =
            await _context.Users
                .FindAsync(userId);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        var permissions =
            user.Role == Roles.Admin
                ? Permissions.All.ToList()
                : await _permissionService
                    .GetPermissionsAsync(userId);

        return Ok(new
        {
            userId,
            username = user.Username,
            permissions
        });
    }

    [HttpPost("users/{userId}/permissions")]
    public async Task<IActionResult> GrantPermission(
        int userId,
        PermissionDto dto)
    {
        var error = CheckAdmin();

        if (error != null)
            return error;

        var admin =
            HttpContext.GetCurrentUser()!;

        var targetUser =
            await _context.Users
                .FindAsync(userId);

        if (targetUser == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        if (targetUser.Role == Roles.Admin)
        {
            return BadRequest(new
            {
                message =
                    "Admin already has all permissions."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Permission) ||
            !Permissions.All.Contains(dto.Permission))
        {
            return BadRequest(new
            {
                message =
                    "Invalid permission.",

                allowedPermissions =
                    Permissions.All
            });
        }

        await _permissionService
            .GrantPermissionAsync(
                userId,
                dto.Permission,
                admin.Id);

        await _auditService.LogAsync(
            admin.Id,
            admin.Username,
            "GRANT_PERMISSION",
            $"Granted {dto.Permission} to {targetUser.Username}."
        );

        return Ok(new
        {
            message =
                "Permission granted.",

            user =
                targetUser.Username,

            permission =
                dto.Permission
        });
    }

    [HttpDelete(
        "users/{userId}/permissions/{permission}")]
    public async Task<IActionResult> RemovePermission(
        int userId,
        string permission)
    {
        var error = CheckAdmin();

        if (error != null)
            return error;

        var admin =
            HttpContext.GetCurrentUser()!;

        var targetUser =
            await _context.Users
                .FindAsync(userId);

        if (targetUser == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        if (targetUser.Role == Roles.Admin)
        {
            return BadRequest(new
            {
                message =
                    "Permissions cannot be removed from an admin."
            });
        }

        var removed =
            await _permissionService
                .RemovePermissionAsync(
                    userId,
                    permission);

        if (!removed)
        {
            return NotFound(new
            {
                message =
                    "Permission not found."
            });
        }

        await _auditService.LogAsync(
            admin.Id,
            admin.Username,
            "REMOVE_PERMISSION",
            $"Removed {permission} from {targetUser.Username}."
        );

        return Ok(new
        {
            message =
                "Permission removed."
        });
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int take = 100,
        [FromQuery] string? username = null,
        [FromQuery] string? action = null)
    {
        var error = CheckAdmin();

        if (error != null)
            return error;

        if (take < 1)
            take = 1;

        if (take > 500)
            take = 500;

        var query =
            _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(a =>
                a.Username == username);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalizedAction =
                action.Trim().ToUpperInvariant();

            query = query.Where(a =>
                a.Action == normalizedAction);
        }

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();

        return Ok(logs);
    }
}