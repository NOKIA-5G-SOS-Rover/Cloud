namespace backend.Models;

public class SOSAlert
{
    public int Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public double LocationX { get; set; }
    public double LocationY { get; set; }

    public double BoundingBoxWidth { get; set; }
    public double BoundingBoxHeight { get; set; }

    public double ConfidenceScore { get; set; }
}