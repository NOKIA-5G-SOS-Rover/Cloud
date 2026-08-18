namespace backend.Services;

public sealed class CameraStreamOptions
{
    public const string SectionName = "Cameras";

    /// <summary>
    /// A camera whose newest frame is older than this is reported as disconnected.
    /// </summary>
    public int OfflineAfterSeconds { get; set; } = 5;

    /// <summary>
    /// Upper bound for a single JPEG frame, applied to both pushed and pulled frames.
    /// </summary>
    public int MaxFrameBytes { get; set; } = 4 * 1024 * 1024;

    public List<CameraSourceOptions> Sources { get; set; } = new();
}

public sealed class CameraSourceOptions
{
    /// <summary>
    /// Path segment the frontend requests, i.e. /stream/{Id}.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Identifier used in SignalR CameraStatusUpdate messages.
    /// </summary>
    public string DisplayId { get; set; } = string.Empty;

    /// <summary>
    /// When set, the backend pulls frames from this URL instead of waiting for the
    /// rover to push them. Either an MJPEG stream or a plain JPEG snapshot endpoint
    /// works; the two are told apart by the upstream's Content-Type.
    /// </summary>
    public string? UpstreamUrl { get; set; }

    /// <summary>
    /// Delay between requests when the upstream serves single snapshots rather than
    /// a continuous MJPEG stream.
    /// </summary>
    public int UpstreamPollIntervalMs { get; set; } = 200;
}