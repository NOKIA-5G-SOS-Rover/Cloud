namespace backend.Models;

public class Device
{
    public int Id { get; set; }

    public string RoverId { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public double BatteryLevel { get; set; }

    public double SignalStrength { get; set; }

}