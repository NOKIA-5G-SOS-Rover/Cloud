using backend.Constants;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class RoverControlService
{
    private readonly AppDbContext _context;
    private readonly PermissionService _permissionService;
    private readonly AuditService _auditService;

    public RoverControlService(
        AppDbContext context,
        PermissionService permissionService,
        AuditService auditService)
    {
        _context = context;
        _permissionService = permissionService;
        _auditService = auditService;
    }

    public async Task<RoverControlSession?>
        GetActiveControlAsync()
    {
        var active = await _context.RoverControlSessions
            .Include(r => r.User)
            .FirstOrDefaultAsync(r =>
                r.EndedAt == null);

        if (active == null)
            return null;

        var timeout =
            TimeSpan.FromMinutes(5);

        var inactiveFor =
            DateTime.UtcNow -
            active.LastActivityAt;

        if (inactiveFor > timeout)
        {
            active.EndedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                active.User.Id,
                active.User.Username,
                "ROVER_CONTROL_TIMEOUT",
                "Rover control released because of inactivity."
            );

            return null;
        }

        return active;
    }

    public async Task<(bool Success, string Message)>
        TakeControlAsync(User user)
    {
        var allowed =
            await _permissionService
                .HasPermissionAsync(
                    user,
                    Permissions.ControlRover);

        if (!allowed)
        {
            return (
                false,
                "You do not have permission to control the rover."
            );
        }

        var active =
            await GetActiveControlAsync();

        if (active != null)
        {
            // Userul deja controleaza roverul
            if (active.UserId == user.Id)
            {
                active.LastActivityAt =
                    DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return (
                    true,
                    "You already control the rover."
                );
            }

            // Adminul poate prelua controlul
            if (user.Role == Roles.Admin)
            {
                var previousUser =
                    active.User.Username;

                active.EndedAt =
                    DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    user.Id,
                    user.Username,
                    "ROVER_CONTROL_OVERRIDE",
                    $"Admin took rover control from {previousUser}."
                );
            }
            else
            {
                return (
                    false,
                    $"Rover is currently controlled by {active.User.Username}."
                );
            }
        }

        var control =
            new RoverControlSession
            {
                UserId = user.Id,
                StartedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            };

        _context.RoverControlSessions
            .Add(control);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            user.Id,
            user.Username,
            "TAKE_ROVER_CONTROL",
            "User acquired rover control."
        );

        return (
            true,
            "Rover control granted."
        );
    }

    public async Task<bool>
        HasControlAsync(User user)
    {
        var active =
            await GetActiveControlAsync();

        if (active == null)
            return false;

        return active.UserId == user.Id;
    }

    public async Task UpdateActivityAsync(
        User user)
    {
        var active =
            await _context.RoverControlSessions
                .FirstOrDefaultAsync(r =>
                    r.UserId == user.Id &&
                    r.EndedAt == null);

        if (active == null)
            return;

        active.LastActivityAt =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool>
        ReleaseControlAsync(User user)
    {
        var active =
            await GetActiveControlAsync();

        if (active == null)
            return false;

        if (active.UserId != user.Id &&
            user.Role != Roles.Admin)
        {
            return false;
        }

        var controlledBy =
            active.User.Username;

        active.EndedAt =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (active.UserId == user.Id)
        {
            await _auditService.LogAsync(
                user.Id,
                user.Username,
                "RELEASE_ROVER_CONTROL",
                "User released rover control."
            );
        }
        else
        {
            await _auditService.LogAsync(
                user.Id,
                user.Username,
                "ADMIN_RELEASE_ROVER_CONTROL",
                $"Admin released rover control held by {controlledBy}."
            );
        }

        return true;
    }
}