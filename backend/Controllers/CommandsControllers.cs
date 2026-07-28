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
    public async Task<ActionResult> SendCommand(SendCommandDto dto)
    {
        await _hubContext.Clients.Group($"rover-{dto.RoverId}")
            .SendAsync("ReceiveCommand", dto);

        return Ok(new
        {
            message = "Command sent to robot.",
            command = dto
        });
    }
}