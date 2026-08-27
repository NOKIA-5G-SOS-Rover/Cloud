using backend.Constants;
using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Hubs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
    private readonly IHubContext<DashboardHub> _hubContext;

    public AdminController(
        AppDbContext context,
        PermissionService permissionService,
        SessionService sessionService,
        AuditService auditService,
        IHubContext<DashboardHub> hubContext)
    {
        _context = context;
        _permissionService = permissionService;
        _sessionService = sessionService;
        _auditService = auditService;
        _hubContext = hubContext;
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

        // Map frontend permission keys to backend constant strings
        var permissionKey = dto.Permission switch
        {
            "view-overview" => Permissions.ViewDashboard,
            "view-cameras" => Permissions.ViewCamera,
            "view-past-alerts" => Permissions.ViewEvents,
            "respond-to-alerts" => Permissions.UpdateEvents,
            "manual-rover-control" => Permissions.ControlRover,
            "motor-power-controls" => Permissions.EmergencyStop,
            "access-admin" => Roles.Admin,
            "change-operating-mode" => "ChangeOperatingMode",
            _ => dto.Permission
        };

        if (string.IsNullOrWhiteSpace(permissionKey) ||
            (!Permissions.All.Contains(permissionKey) && permissionKey != Roles.Admin && permissionKey != "ChangeOperatingMode"))
        {
            return BadRequest(new
            {
                message =
                    "Invalid permission.",

                received = dto.Permission,

                allowedPermissions =
                    Permissions.All
            });
        }

        // If it's the admin role mapping, handle user role promotion instead of permission table lookup
        if (permissionKey == Roles.Admin)
        {
            targetUser.Role = Roles.Admin;
            await _context.SaveChangesAsync();
        }
        else
        {
            await _permissionService
                .GrantPermissionAsync(
                    userId,
                    permissionKey,
                    admin.Id);
        }

        await _auditService.LogAsync(
            admin.Id,
            admin.Username,
            "GRANT_PERMISSION",
            $"Granted {permissionKey} to {targetUser.Username}."
        );

        // Broadcast permission change in real-time via SignalR
        var currentPermissions = targetUser.Role == Roles.Admin ? Permissions.All.ToList() : await _permissionService.GetPermissionsAsync(userId);
        await _hubContext.Clients.All.SendAsync("PermissionsUpdated", new
        {
            id = userId,
            username = targetUser.Username,
            permissions = currentPermissions
        });

        return Ok(new
        {
            message =
                "Permission granted.",

            user =
                targetUser.Username,

            permission =
                permissionKey
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

        if (targetUser.Role == Roles.Admin && permission != "access-admin")
        {
            return BadRequest(new
            {
                message =
                    "Permissions cannot be removed from an admin."
            });
        }

        // Map frontend permission keys to backend constant strings
        var permissionKey = permission switch
        {
            "view-overview" => Permissions.ViewDashboard,
            "view-cameras" => Permissions.ViewCamera,
            "view-past-alerts" => Permissions.ViewEvents,
            "respond-to-alerts" => Permissions.UpdateEvents,
            "manual-rover-control" => Permissions.ControlRover,
            "motor-power-controls" => Permissions.EmergencyStop,
            "access-admin" => Roles.Admin,
            "change-operating-mode" => "ChangeOperatingMode",
            _ => permission
        };

        bool removed = true;
        if (permissionKey == Roles.Admin)
        {
            targetUser.Role = Roles.User;
            await _context.SaveChangesAsync();
        }
        else
        {
            removed =
                await _permissionService
                    .RemovePermissionAsync(
                        userId,
                        permissionKey);
        }

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
            $"Removed {permissionKey} from {targetUser.Username}."
        );

        // Broadcast permission change in real-time via SignalR
        var currentPermissions = targetUser.Role == Roles.Admin ? Permissions.All.ToList() : await _permissionService.GetPermissionsAsync(userId);
        await _hubContext.Clients.All.SendAsync("PermissionsUpdated", new
        {
            id = userId,
            username = targetUser.Username,
            permissions = currentPermissions
        });

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

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateAccountDto dto)
    {
        var error = CheckAdmin();
        if (error != null) return error;

        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Username and password are required." });

        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            return BadRequest(new { message = "Username already exists." });

        var admin = HttpContext.GetCurrentUser()!;

        var user = new User
        {
            Username = dto.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = Roles.User,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (dto.Permissions != null && dto.Permissions.Any())
        {
            foreach (var perm in dto.Permissions)
            {
                var mappedPerm = perm switch
                {
                    "view-overview" => Permissions.ViewDashboard,
                    "view-cameras" => Permissions.ViewCamera,
                    "view-past-alerts" => Permissions.ViewEvents,
                    "respond-to-alerts" => Permissions.UpdateEvents,
                    "manual-rover-control" => Permissions.ControlRover,
                    "motor-power-controls" => Permissions.EmergencyStop,
                    _ => perm
                };

                if (Permissions.All.Contains(mappedPerm))
                {
                    _context.UserPermissions.Add(new UserPermission
                    {
                        UserId = user.Id,
                        Permission = mappedPerm,
                        GrantedByAdminId = admin.Id
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            id = user.Id, 
            username = user.Username,
            role = user.Role,
            enabled = true,
            roverIds = new[] { "sanzi" },
            permissions = dto.Permissions ?? new List<string>(),
            createdAt = user.CreatedAt,
            lastLogin = (DateTime?)null
        });
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var error = CheckAdmin();
        if (error != null) return error;

        var targetUser = await _context.Users.FindAsync(userId);
        if (targetUser == null) return NotFound(new { message = "User not found." });

        if (targetUser.Role == Roles.Admin)
            return BadRequest(new { message = "Cannot delete the admin account." });

        _context.Users.Remove(targetUser);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User deleted successfully." });
    }
}
