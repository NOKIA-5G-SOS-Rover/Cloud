namespace backend.Models;

public class Telemetry
{
    public int Id { get; set; }

    public string RoverId { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public double BatteryLevel { get; set; }

    public double? SignalStrength { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}