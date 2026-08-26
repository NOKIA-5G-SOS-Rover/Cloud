namespace backend.Dtos;

public class CreateAccountDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}