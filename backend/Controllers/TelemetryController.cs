using backend.Data;
using backend.Dtos;
using backend.Hubs;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("telemetry")]
public class TelemetryController : ControllerBase
{
    private readonly AppDbContext _database;
    private readonly IHubContext<DashboardHub> _hubContext;

    public TelemetryController(
        AppDbContext database,
        IHubContext<DashboardHub> hubContext)
    {
        _database = database;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<ActionResult<Telemetry>> CreateTelemetry(
        [FromBody] TelemetryDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.RoverId))
        {
            return BadRequest(new
            {
                message = "RoverId is required."
            });
        }

        if (dto.Battery < 0 || dto.Battery > 100)
        {
            return BadRequest(new
            {
                message = "Battery must be between 0 and 100."
            });
        }

        var telemetry = new Telemetry
        {
            RoverId = dto.RoverId.Trim(),
            Timestamp = DateTimeOffset.UtcNow,
            BatteryLevel = dto.Battery,
            SignalStrength = dto.SignalStrength
        };

        _database.Telemetries.Add(telemetry);

        await _database.SaveChangesAsync(
            cancellationToken);

        await _hubContext.Clients
            .Group($"rover-{telemetry.RoverId}")
            .SendAsync(
                "ReceiveTelemetry",
                telemetry,
                cancellationToken
            );

        return Ok(telemetry);
    }

    [HttpGet("{roverId}/latest")]
    public async Task<ActionResult<Telemetry>> GetLatestTelemetry(
        string roverId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roverId))
        {
            return BadRequest(new
            {
                message = "RoverId is required."
            });
        }

        var normalizedRoverId = roverId.Trim();

        var telemetry = await _database.Telemetries
            .AsNoTracking()
            .Where(t =>
                t.RoverId == normalizedRoverId)
            .OrderByDescending(t =>
                t.Timestamp)
            .FirstOrDefaultAsync(
                cancellationToken);

        if (telemetry == null)
        {
            return NotFound(new
            {
                message =
                    "No telemetry found for this rover."
            });
        }

        return Ok(telemetry);
    }

    [HttpGet("{roverId}/history")]
    public async Task<ActionResult<List<Telemetry>>> GetTelemetryHistory(
        string roverId,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roverId))
        {
            return BadRequest(new
            {
                message = "RoverId is required."
            });
        }

        if (take < 1)
            take = 1;

        if (take > 500)
            take = 500;

        var normalizedRoverId = roverId.Trim();

        var telemetry = await _database.Telemetries
            .AsNoTracking()
            .Where(t =>
                t.RoverId == normalizedRoverId)
            .OrderByDescending(t =>
                t.Timestamp)
            .Take(take)
            .ToListAsync(
                cancellationToken);

        return Ok(telemetry);
    }
}