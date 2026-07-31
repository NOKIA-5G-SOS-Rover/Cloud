namespace backend.Services;

public interface IRoverCommandSender
{
    Task SendAsync(
        string roverId,
        string command,
        int? speed,
        float? degrees,
        CancellationToken cancellationToken
    );
}