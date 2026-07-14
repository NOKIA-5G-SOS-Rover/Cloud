namespace backend.Models;

public class Event
{
    public int Id { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public string RoverId { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public string AlertType { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public DateTimeOffset DetectedAt { get; set; }

    public double LocationX { get; set; }

    public double LocationY { get; set; }

    public double BoundingBoxWidth { get; set; }

    public double BoundingBoxHeight { get; set; }

    public double ConfidenceScore { get; set; }

    public bool MotorHaltRequested { get; set; }
    
    public string? ImageUrl { get; set; }
}