namespace backend.Dtos;

public class TelemetryDto
{
    public string RoverId { get; set; } = string.Empty;

    public double Battery { get; set; }

    public double? SignalStrength { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}