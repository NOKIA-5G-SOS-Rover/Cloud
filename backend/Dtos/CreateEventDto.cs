using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class CreateEventDto
{
    [Required]
    public string RoverId { get; set; } = string.Empty;

    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    public string AlertType { get; set; } = string.Empty;

    [Required]
    public string Source { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset DetectedAt { get; set; }

    [Range(0, double.MaxValue)]
    public double LocationX { get; set; }

    [Range(0, double.MaxValue)]
    public double LocationY { get; set; }

    [Range(1, double.MaxValue)]
    public double BoundingBoxWidth { get; set; }

    [Range(1, double.MaxValue)]
    public double BoundingBoxHeight { get; set; }

    [Range(0, 1)]
    public double ConfidenceScore { get; set; }

    public bool MotorHaltRequested { get; set; }
}