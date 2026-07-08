namespace backend.Dtos;

public class CreateSOSAlertDto
{
    public double LocationX { get; set; }
    public double LocationY { get; set; }

    public double BoundingBoxWidth { get; set; }
    public double BoundingBoxHeight { get; set; }

    public double ConfidenceScore { get; set; }
}