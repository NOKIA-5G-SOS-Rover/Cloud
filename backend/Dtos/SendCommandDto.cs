using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class SendCommandDto
{
    [Required]
    [StringLength(100)]
    public string RoverId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Command { get; set; } = string.Empty;

    [Range(0, 100)]
    public int? Speed { get; set; }

    public float? Degrees { get; set; }
}