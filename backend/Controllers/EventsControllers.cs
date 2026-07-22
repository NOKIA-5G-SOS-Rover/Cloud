using backend.Data;
using backend.Dtos;
using backend.Hubs;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _database;
    private readonly IHubContext<DashboardHub> _hubContext;

    public EventsController(AppDbContext database, IHubContext<DashboardHub> hubContext)
    {
        _database = database;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<Event>>> GetAlerts()
    {
        var alerts = await _database.Events
            .OrderByDescending(alert => alert.Timestamp)
            .ToListAsync();

        return Ok(alerts);
    }

    [HttpPost]
    public async Task<ActionResult<Event>> CreateEvent(CreateEventDto dto)
    {
        var alert = new Event
{
    Timestamp = DateTimeOffset.UtcNow,
    RoverId = dto.RoverId,
    SessionId = dto.SessionId,
    AlertType = dto.AlertType,
    Source = dto.Source,
    DetectedAt = dto.DetectedAt,
    LocationX = dto.LocationX,
    LocationY = dto.LocationY,
    BoundingBoxWidth = dto.BoundingBoxWidth,
    BoundingBoxHeight = dto.BoundingBoxHeight,
    ConfidenceScore = dto.ConfidenceScore,
    MotorHaltRequested = dto.MotorHaltRequested,
    InjuryClass = dto.InjuryClass,
    CameraId = dto.CameraId,
    Status = dto.Status
};

        _database.Events.Add(alert);
        await _database.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("ReceiveAlert", alert);

        return Ok(alert);
    }

    [HttpGet("{id}")]
public async Task<ActionResult<Event>> GetEventById(int id)
{
    var foundEvent = await _database.Events.FindAsync(id);

    if (foundEvent == null)
    {
        return NotFound();
    }

    return Ok(foundEvent);
}
[HttpPost("{id}/image")]
[Consumes("multipart/form-data")]
public async Task<ActionResult> UploadEventImage(int id, IFormFile image)
{
    var foundEvent = await _database.Events.FindAsync(id);

    if (foundEvent == null)
    {
        return NotFound(new
        {
            message = "Event not found."
        });
    }

    if (image == null || image.Length == 0)
    {
        return BadRequest(new
        {
            message = "Image file is required."
        });
    }

    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
    var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

    if (!allowedExtensions.Contains(extension))
    {
        return BadRequest(new
        {
            message = "Invalid image format. Only .jpg, .jpeg and .png are allowed."
        });
    }

    const long maxFileSize = 5 * 1024 * 1024;

    if (image.Length > maxFileSize)
    {
        return BadRequest(new
        {
            message = "Image is too large. Maximum allowed size is 5 MB."
        });
    }

    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

    Directory.CreateDirectory(uploadsFolder);

    var fileName = $"{Guid.NewGuid()}{extension}";
    var filePath = Path.Combine(uploadsFolder, fileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await image.CopyToAsync(stream);
    }

    foundEvent.ImageUrl = $"/uploads/{fileName}";

    await _database.SaveChangesAsync();

    return Ok(new
    {
        imageUrl = foundEvent.ImageUrl
    });
}
}