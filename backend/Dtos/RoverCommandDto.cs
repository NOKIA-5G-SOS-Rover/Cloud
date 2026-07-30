namespace backend.Dtos;

public class RoverCommandDto
{
    public List<string> Directions { get; set; } = new List<string>();
    public int Speed { get; set; }
}