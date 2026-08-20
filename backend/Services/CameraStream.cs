namespace backend.Services;

public sealed record CameraFrame(byte[] Data, long Sequence, DateTimeOffset ReceivedAt);

/// <summary>
/// Holds the newest frame for one camera and lets any number of viewers wait for the
/// next one. Frames are never queued: a slow viewer skips ahead to the current frame
/// rather than falling further and further behind live.
/// </summary>
public sealed class CameraStream
{
    private readonly TimeSpan _offlineAfter;
    private readonly object _gate = new();

    private CameraFrame? _latest;
    private long _sequence;
    private TaskCompletionSource<CameraFrame> _nextFrame =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CameraStream(CameraSourceOptions source, TimeSpan offlineAfter)
    {
        Id = source.Id;
        DisplayId = string.IsNullOrWhiteSpace(source.DisplayId) ? source.Id : source.DisplayId;
        UpstreamUrl = string.IsNullOrWhiteSpace(source.UpstreamUrl) ? null : source.UpstreamUrl.Trim();
        UpstreamPollInterval = TimeSpan.FromMilliseconds(Math.Max(20, source.UpstreamPollIntervalMs));

        _offlineAfter = offlineAfter;
    }

    public string Id { get; }

    public string DisplayId { get; }

    public string? UpstreamUrl { get; }

    public TimeSpan UpstreamPollInterval { get; }

    public CameraFrame? Latest
    {
        get
        {
            lock (_gate)
            {
                return _latest;
            }
        }
    }

    public bool IsOnline
    {
        get
        {
            var latest = Latest;

            return latest is not null
                && DateTimeOffset.UtcNow - latest.ReceivedAt <= _offlineAfter;
        }
    }

    public void Publish(byte[] jpeg)
    {
        TaskCompletionSource<CameraFrame> pendingViewers;
        CameraFrame frame;

        lock (_gate)
        {
            frame = new CameraFrame(jpeg, ++_sequence, DateTimeOffset.UtcNow);
            _latest = frame;

            pendingViewers = _nextFrame;
            _nextFrame = new TaskCompletionSource<CameraFrame>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
        }

        pendingViewers.TrySetResult(frame);
    }

    /// <summary>
    /// Returns the newest frame with a sequence above <paramref name="afterSequence"/>,
    /// waiting up to <paramref name="timeout"/> for one to arrive. Returns null on timeout.
    /// </summary>
    public async Task<CameraFrame?> ReadFrameAsync(
        long afterSequence,
        TimeSpan timeout,
        CancellationToken cancellationToken
    )
    {
        Task<CameraFrame> pending;

        lock (_gate)
        {
            if (_latest is { } latest && latest.Sequence > afterSequence)
            {
                return latest;
            }

            pending = _nextFrame.Task;
        }

        try
        {
            return await pending.WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }
}
