using backend.Constants;
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

    private static readonly TimeSpan DuplicateWindow =
        TimeSpan.FromSeconds(5);

    public EventsController(
        AppDbContext database,
        IHubContext<DashboardHub> hubContext)
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
    public async Task<ActionResult<Event>> CreateEvent(
        CreateEventDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.RoverId))
        {
            return BadRequest(new
            {
                message = "RoverId is required."
            });
        }

        var normalizedRoverId = dto.RoverId.Trim();

        var injuryClass = string.IsNullOrWhiteSpace(dto.InjuryClass)
            ? InjuryClasses.Unknown
            : dto.InjuryClass.Trim().ToUpperInvariant();

        if (!InjuryClasses.IsValid(injuryClass))
        {
            return BadRequest(new
            {
                message = "Invalid injuryClass.",
                allowedValues = InjuryClasses.Allowed
            });
        }

        var duplicateAfter =
            DateTimeOffset.UtcNow.Subtract(DuplicateWindow);

        var duplicateExists = await _database.Events
            .AsNoTracking()
            .AnyAsync(
                e =>
                    e.RoverId == normalizedRoverId &&
                    e.AlertType == dto.AlertType &&
                    e.InjuryClass == injuryClass &&
                    e.Timestamp >= duplicateAfter,
                cancellationToken
            );

        if (duplicateExists)
        {
            return Ok(new
            {
                ignored = true,
                reason = "Duplicate event.",
                duplicateWindowSeconds = DuplicateWindow.TotalSeconds
            });
        }

        var alert = new Event
        {
            Timestamp = DateTimeOffset.UtcNow,
            RoverId = normalizedRoverId,
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
            InjuryClass = injuryClass,
            CameraId = dto.CameraId,
            Status = dto.Status
        };

        _database.Events.Add(alert);

        await _database.SaveChangesAsync(
            cancellationToken
        );

        await _hubContext.Clients
            .Group(DashboardHub.DashboardGroup)
            .SendAsync(
                "ReceiveAlert",
                alert,
                cancellationToken
            );

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
    public async Task<ActionResult> UploadEventImage(
        int id,
        IFormFile image)
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

        var allowedExtensions =
            new[] { ".jpg", ".jpeg", ".png" };

        var extension = Path
            .GetExtension(image.FileName)
            .ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                message =
                    "Invalid image format. Only .jpg, .jpeg and .png are allowed."
            });
        }

        const long maxFileSize = 5 * 1024 * 1024;

        if (image.Length > maxFileSize)
        {
            return BadRequest(new
            {
                message =
                    "Image is too large. Maximum allowed size is 5 MB."
            });
        }

        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads"
        );

        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(
            uploadsFolder,
            fileName
        );

        using (var stream =
               new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        foundEvent.ImageUrl =
            $"/uploads/{fileName}";

        await _database.SaveChangesAsync();

        return Ok(new
        {
            imageUrl = foundEvent.ImageUrl
        });
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateEventStatus(
        int id,
        [FromBody] UpdateEventStatusDto dto)
    {
        var foundEvent =
            await _database.Events.FindAsync(id);

        if (foundEvent == null)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        foundEvent.Status = dto.Status;

        await _database.SaveChangesAsync();

        return Ok(new
        {
            message = "Status updated successfully.",
            eventId = foundEvent.Id,
            newStatus = foundEvent.Status
        });
    }
}