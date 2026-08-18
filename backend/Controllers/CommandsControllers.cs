using backend.Dtos;
using backend.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace backend.Controllers;

[ApiController]
[Route("commands")]
public class CommandsController : ControllerBase
{
    private readonly IHubContext<DashboardHub> _hubContext;

    public CommandsController(IHubContext<DashboardHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<ActionResult> SendCommand(
        [FromBody] SendCommandDto dto,
        CancellationToken cancellationToken)
    {
        dto.RoverId = dto.RoverId.Trim();
        dto.Command = dto.Command.Trim().ToUpperInvariant();

        await _hubContext.Clients
            .Group($"rover-{dto.RoverId}")
            .SendAsync(
                "ReceiveCommand",
                dto,
                cancellationToken
            );

        return Accepted(new
        {
            message = "Command published to rover.",
            command = dto
        });
    }
}