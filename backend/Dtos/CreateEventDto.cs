using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class CreateEventDto
{
    [Required]
    [StringLength(100)]
    public string RoverId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string AlertType { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Source { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset DetectedAt { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "LocationX must be greater than or equal to 0.")]
    public double LocationX { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "LocationY must be greater than or equal to 0.")]
    public double LocationY { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "BoundingBoxWidth must be greater than 0.")]
    public double BoundingBoxWidth { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "BoundingBoxHeight must be greater than 0.")]
    public double BoundingBoxHeight { get; set; }

    [Range(0, 1, ErrorMessage = "ConfidenceScore must be between 0 and 1.")]
    public double ConfidenceScore { get; set; }

    public bool MotorHaltRequested { get; set; }

    [Required]
    [StringLength(50)]
    public string InjuryClass { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CameraId { get; set; } = string.Empty;

    [StringLength(50)]
    public string Status { get; set; } = "NEW";
}