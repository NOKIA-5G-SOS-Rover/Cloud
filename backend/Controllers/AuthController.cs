using backend.Constants;
using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;


namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly SessionService _sessionService;
    private readonly PermissionService _permissionService;
    private readonly AuditService _auditService;

    public AuthController(
        AppDbContext context,
        SessionService sessionService,
        PermissionService permissionService,
        AuditService auditService)
    {
        _context = context;
        _sessionService = sessionService;
        _permissionService = permissionService;
        _auditService = auditService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new
            {
                message = "Username and password are required."
            });
        }

        if (dto.Password.Length < 6)
        {
            return BadRequest(new
            {
                message = "Password must have at least 6 characters."
            });
        }

        var username = dto.Username.Trim();

        var exists = await _context.Users
            .AnyAsync(u => u.Username == username);

        if (exists)
        {
            return BadRequest(new
            {
                message = "Username already exists."
            });
        }

        var user = new User
        {
            Username = username,

            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    dto.Password),

            Role = Roles.User,

            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        _context.UserPermissions.AddRange(
            new UserPermission
            {
                UserId = user.Id,
                Permission = Permissions.ViewDashboard,
                GrantedByAdminId = user.Id
            },
            new UserPermission
            {
                UserId = user.Id,
                Permission = Permissions.ViewCamera,
                GrantedByAdminId = user.Id
            },
            new UserPermission
            {
                UserId = user.Id,
                Permission = Permissions.ViewEvents,
                GrantedByAdminId = user.Id
            });

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            user.Id,
            user.Username,
            "REGISTER",
            "User account created."
        );

        return Ok(new
        {
            message = "Account created successfully."
        });
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new
            {
                message = "Username and password are required."
            });
        }

        var username = dto.Username.Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Username == username);

        if (user == null)
        {
            await _auditService.LogAsync(
                null,
                username,
                "LOGIN_FAILED",
                "Login failed because username does not exist."
            );

            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        var validPassword =
            BCrypt.Net.BCrypt.Verify(
                dto.Password,
                user.PasswordHash);

        if (!validPassword)
        {
            await _auditService.LogAsync(
                user.Id,
                user.Username,
                "LOGIN_FAILED",
                "Login failed because password was invalid."
            );

            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        var session =
            await _sessionService
                .CreateSessionAsync(user);

        await _auditService.LogAsync(
            user.Id,
            user.Username,
            "LOGIN",
            "User logged in successfully."
        );

        var permissions =
            user.Role == Roles.Admin
                ? Permissions.All.ToList()
                : await _permissionService
                    .GetPermissionsAsync(user.Id);

        return Ok(new
        {
            sessionId = session.SessionId,

            user = new
            {
                id = user.Id,
                username = user.Username,
                role = user.Role,
                permissions
            },

            connectedAt =
                session.ConnectedAt,

            expiresAt =
                session.ExpiresAt
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user =
            HttpContext.GetCurrentUser();

        var session =
            HttpContext.GetCurrentSession();

        if (user == null ||
            session == null)
        {
            return Unauthorized(new
            {
                message =
                    "Invalid or expired session."
            });
        }

        var permissions =
            user.Role == Roles.Admin
                ? Permissions.All.ToList()
                : await _permissionService
                    .GetPermissionsAsync(user.Id);

        var connectedFor =
            DateTime.UtcNow -
            session.ConnectedAt;

        return Ok(new
        {
            id = user.Id,

            username =
                user.Username,

            role =
                user.Role,

            permissions,

            session = new
            {
                connectedAt =
                    session.ConnectedAt,

                connectedForSeconds =
                    (long)connectedFor.TotalSeconds,

                lastActivityAt =
                    session.LastActivityAt,

                expiresAt =
                    session.ExpiresAt
            }
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var user =
            HttpContext.GetCurrentUser();

        var session =
            HttpContext.GetCurrentSession();

        if (user == null ||
            session == null)
        {
            return Unauthorized(new
            {
                message =
                    "Invalid or expired session."
            });
        }

        var loggedOut =
            await _sessionService
                .LogoutAsync(
                    session.SessionId);

        if (!loggedOut)
        {
            return BadRequest(new
            {
                message =
                    "Logout failed."
            });
        }

        await _auditService.LogAsync(
            user.Id,
            user.Username,
            "LOGOUT",
            "User logged out successfully."
        );

        return Ok(new
        {
            message =
                "Logged out successfully."
        });
    }
}
