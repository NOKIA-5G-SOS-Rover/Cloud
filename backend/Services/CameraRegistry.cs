using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace backend.Services;

public sealed class CameraRegistry
{
    private readonly Dictionary<string, CameraStream> _cameras;

    public CameraRegistry(IOptions<CameraStreamOptions> options)
    {
        var settings = options.Value;
        var offlineAfter = TimeSpan.FromSeconds(Math.Max(1, settings.OfflineAfterSeconds));

        _cameras = settings.Sources
            .Where(source => !string.IsNullOrWhiteSpace(source.Id))
            .Select(source => new CameraStream(source, offlineAfter))
            .ToDictionary(camera => camera.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<CameraStream> All => _cameras.Values;

    public bool TryGet(string? cameraId, [NotNullWhen(true)] out CameraStream? camera)
    {
        return _cameras.TryGetValue(cameraId ?? string.Empty, out camera);
    }
}
