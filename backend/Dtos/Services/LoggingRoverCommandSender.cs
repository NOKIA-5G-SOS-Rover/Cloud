namespace backend.Services;

public class LoggingRoverCommandSender : IRoverCommandSender
{
    private readonly ILogger<LoggingRoverCommandSender> _logger;

    public LoggingRoverCommandSender(
        ILogger<LoggingRoverCommandSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        string roverId,
        string command,
        int? speed,
        float? degrees,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Rover command: RoverId={RoverId}, Command={Command}, Speed={Speed}, Degrees={Degrees}",
            roverId,
            command,
            speed,
            degrees
        );

        return Task.CompletedTask;
    }
}