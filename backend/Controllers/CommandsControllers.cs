using backend.Constants;
using backend.Dtos;
using backend.Extensions;
using backend.Hubs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace backend.Controllers;

[ApiController]
[Route("commands")]
public class CommandsController : ControllerBase
{
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly PermissionService _permissionService;
    private readonly RoverControlService _roverControlService;
    private readonly AuditService _auditService;

    public CommandsController(
        IHubContext<DashboardHub> hubContext,
        PermissionService permissionService,
        RoverControlService roverControlService,
        AuditService auditService)
    {
        _hubContext = hubContext;
        _permissionService = permissionService;
        _roverControlService = roverControlService;
        _auditService = auditService;
    }

    [HttpPost]
    public async Task<ActionResult> SendCommand(
        [FromBody] SendCommandDto dto,
        CancellationToken cancellationToken)
    {
        var user = HttpContext.GetCurrentUser();

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "You are not logged in."
            });
        }

        var hasPermission =
            await _permissionService.HasPermissionAsync(
                user,
                Permissions.ControlRover
            );

        if (!hasPermission)
        {
            return StatusCode(403, new
            {
                message =
                    "You do not have permission to control the rover."
            });
        }

        var hasControl =
            await _roverControlService.HasControlAsync(user);

        if (!hasControl)
        {
            return StatusCode(403, new
            {
                message =
                    "You do not currently control the rover. Take control first."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.RoverId) ||
            string.IsNullOrWhiteSpace(dto.Command))
        {
            return BadRequest(new
            {
                message = "RoverId and Command are required."
            });
        }

        dto.RoverId =
            dto.RoverId.Trim();

        dto.Command =
            dto.Command
                .Trim()
                .ToUpperInvariant();

        await _hubContext.Clients
            .Group($"rover-{dto.RoverId}")
            .SendAsync(
                "ReceiveCommand",
                dto,
                cancellationToken
            );

        await _auditService.LogAsync(
            user.Id,
            user.Username,
            "SEND_COMMAND",
            $"Sent command {dto.Command} to rover {dto.RoverId}."
        );

        await _roverControlService
            .UpdateActivityAsync(user);

        return Accepted(new
        {
            message = "Command published to rover.",
            sentBy = user.Username,
            roverId = dto.RoverId,
            command = dto.Command
        });
    }
}
