using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace backend.Services;

/// <summary>
/// Keeps a connection open to every camera that has an UpstreamUrl configured and
/// republishes what it reads. This is the "backend pulls" ingest path, used when the
/// camera servers (ports 8081 / 8082) are reachable from the API container. Cameras
/// without an UpstreamUrl are fed by the rover pushing to /stream/{id}/frame instead.
/// </summary>
public sealed class CameraPullWorker : BackgroundService
{
    public const string HttpClientName = "camera-upstream";

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan SnapshotRequestTimeout = TimeSpan.FromSeconds(10);

    private readonly CameraRegistry _cameras;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CameraStreamOptions _options;
    private readonly ILogger<CameraPullWorker> _logger;

    public CameraPullWorker(
        CameraRegistry cameras,
        IHttpClientFactory httpClientFactory,
        IOptions<CameraStreamOptions> options,
        ILogger<CameraPullWorker> logger
    )
    {
        _cameras = cameras;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pumps = _cameras.All
            .Where(camera => camera.UpstreamUrl is not null)
            .Select(camera => PumpAsync(camera, stoppingToken))
            .ToArray();

        if (pumps.Length == 0)
        {
            _logger.LogInformation(
                "No camera upstream URLs configured, waiting for pushed frames on /stream/{{cameraId}}/frame."
            );

            return Task.CompletedTask;
        }

        return Task.WhenAll(pumps);
    }

    private async Task PumpAsync(CameraStream camera, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUpstreamAsync(camera, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Camera {CameraId}: upstream {UpstreamUrl} failed, retrying in {RetrySeconds}s.",
                    camera.Id,
                    camera.UpstreamUrl,
                    RetryDelay.TotalSeconds
                );
            }

            try
            {
                await Task.Delay(RetryDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task ConsumeUpstreamAsync(CameraStream camera, CancellationToken stoppingToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);

        using var response = await client.GetAsync(
            camera.UpstreamUrl,
            HttpCompletionOption.ResponseHeadersRead,
            stoppingToken
        );

        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType;

        if (IsMultipart(contentType))
        {
            await ConsumeMjpegAsync(camera, contentType!, response, stoppingToken);
            return;
        }

        await PollSnapshotsAsync(camera, client, response, stoppingToken);
    }

    private async Task ConsumeMjpegAsync(
        CameraStream camera,
        MediaTypeHeaderValue contentType,
        HttpResponseMessage response,
        CancellationToken stoppingToken
    )
    {
        var boundary = contentType.Parameters
            .FirstOrDefault(parameter => parameter.Name.Equals("boundary", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidDataException(
                $"Upstream {camera.UpstreamUrl} returned multipart content without a boundary."
            );
        }

        _logger.LogInformation(
            "Camera {CameraId}: reading MJPEG stream from {UpstreamUrl}.",
            camera.Id,
            camera.UpstreamUrl
        );

        await using var stream = await response.Content.ReadAsStreamAsync(stoppingToken);
        var reader = new MjpegStreamReader(stream, boundary, _options.MaxFrameBytes);

        while (!stoppingToken.IsCancellationRequested)
        {
            Publish(camera, await reader.ReadFrameAsync(stoppingToken));
        }
    }

    private async Task PollSnapshotsAsync(
        CameraStream camera,
        HttpClient client,
        HttpResponseMessage firstResponse,
        CancellationToken stoppingToken
    )
    {
        _logger.LogInformation(
            "Camera {CameraId}: polling snapshots from {UpstreamUrl} every {IntervalMs}ms.",
            camera.Id,
            camera.UpstreamUrl,
            camera.UpstreamPollInterval.TotalMilliseconds
        );

        Publish(camera, await firstResponse.Content.ReadAsByteArrayAsync(stoppingToken));

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(camera.UpstreamPollInterval, stoppingToken);

            using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            requestTimeout.CancelAfter(SnapshotRequestTimeout);

            Publish(camera, await client.GetByteArrayAsync(camera.UpstreamUrl, requestTimeout.Token));
        }
    }

    private void Publish(CameraStream camera, byte[] frame)
    {
        if (frame.Length == 0)
        {
            return;
        }

        if (frame.Length > _options.MaxFrameBytes)
        {
            _logger.LogWarning(
                "Camera {CameraId}: dropped a {ByteCount} byte upstream frame over the size limit.",
                camera.Id,
                frame.Length
            );

            return;
        }

        // An upstream that answers with an HTML error page would otherwise keep the
        // camera looking online while the browser renders nothing.
        if (frame.Length < 4 || frame[0] != 0xFF || frame[1] != 0xD8)
        {
            _logger.LogWarning(
                "Camera {CameraId}: upstream {UpstreamUrl} returned {ByteCount} bytes that are not a JPEG.",
                camera.Id,
                camera.UpstreamUrl,
                frame.Length
            );

            return;
        }

        camera.Publish(frame);
    }

    private static bool IsMultipart(MediaTypeHeaderValue? contentType)
    {
        return contentType?.MediaType?.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) == true;
    }
}