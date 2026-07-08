using backend.Data;
using backend.Dtos;
using backend.Hubs;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly AppDatabase _database;
    private readonly IHubContext<DashboardHub> _hubContext;

    public AlertsController(AppDatabase database, IHubContext<DashboardHub> hubContext)
    {
        _database = database;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<SOSAlert>>> GetAlerts()
    {
        var alerts = await _database.SOSAlerts
            .OrderByDescending(alert => alert.Timestamp)
            .ToListAsync();

        return Ok(alerts);
    }

    [HttpPost]
    public async Task<ActionResult<SOSAlert>> CreateAlert(CreateSOSAlertDto dto)
    {
        var alert = new SOSAlert
        {
            Timestamp = DateTimeOffset.UtcNow,
            LocationX = dto.LocationX,
            LocationY = dto.LocationY,
            BoundingBoxWidth = dto.BoundingBoxWidth,
            BoundingBoxHeight = dto.BoundingBoxHeight,
            ConfidenceScore = dto.ConfidenceScore
        };

        _database.SOSAlerts.Add(alert);
        await _database.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("ReceiveAlert", alert);

        return Ok(alert);
    }
}