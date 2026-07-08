namespace backend.Models;

public class RoverSession
{
    public int Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public double BatteryLevel { get; set; }
    public double SignalStrength { get; set; }
}