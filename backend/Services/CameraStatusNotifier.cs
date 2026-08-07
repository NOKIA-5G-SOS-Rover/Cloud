using backend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace backend.Services;

/// <summary>
/// Pushes a CameraStatusUpdate to the dashboard whenever a camera starts or stops
/// delivering frames, so the UI can swap between the live tile and the "signal lost"
/// tile without polling.
/// </summary>
public sealed class CameraStatusNotifier : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    private readonly Dictionary<string, bool> _lastBroadcast = new(StringComparer.OrdinalIgnoreCase);
    private readonly CameraRegistry _cameras;
    private readonly IHubContext<DashboardHub> _hub;
    private readonly ILogger<CameraStatusNotifier> _logger;

    public CameraStatusNotifier(
        CameraRegistry cameras,
        IHubContext<DashboardHub> hub,
        ILogger<CameraStatusNotifier> logger
    )
    {
        _cameras = cameras;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                foreach (var camera in _cameras.All)
                {
                    await BroadcastIfChangedAsync(camera, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private async Task BroadcastIfChangedAsync(CameraStream camera, CancellationToken stoppingToken)
    {
        var isOnline = camera.IsOnline;

        if (_lastBroadcast.TryGetValue(camera.Id, out var previous) && previous == isOnline)
        {
            return;
        }

        _lastBroadcast[camera.Id] = isOnline;

        _logger.LogInformation(
            "Camera {CameraId} is now {State}.",
            camera.Id,
            isOnline ? "online" : "offline"
        );

        await _hub.Clients.All.SendAsync(
            "CameraStatusUpdate",
            new
            {
                cameraId = camera.DisplayId,
                streamId = camera.Id,
                isConnected = isOnline
            },
            stoppingToken
        );
    }
}
