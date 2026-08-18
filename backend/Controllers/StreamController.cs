using System.Text;
using backend.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace backend.Controllers;

[ApiController]
[Route("stream")]
public class StreamController : ControllerBase
{
    private const string MultipartBoundary = "roverframe";

    /// <summary>
    /// How long a viewer waits for a new frame before re-checking whether the camera
    /// is still alive. Well above the rover's ~5fps push rate so a healthy stream
    /// never trips it.
    /// </summary>
    private static readonly TimeSpan FrameWaitTimeout = TimeSpan.FromSeconds(2);

    private static readonly byte[] PartSeparator = Encoding.ASCII.GetBytes("\r\n");

    private readonly CameraRegistry _cameras;
    private readonly CameraStreamOptions _options;
    private readonly ILogger<StreamController> _logger;

    public StreamController(
        CameraRegistry cameras,
        IOptions<CameraStreamOptions> options,
        ILogger<StreamController> logger
    )
    {
        _cameras = cameras;
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet("status")]
    public ActionResult GetStatus()
    {
        var statuses = _cameras.All.Select(camera => new
        {
            cameraId = camera.DisplayId,
            streamId = camera.Id,
            isConnected = camera.IsOnline,
            lastFrameAt = camera.Latest?.ReceivedAt,
            streamUrl = $"/stream/{camera.Id}"
        });

        return Ok(statuses);
    }

    /// <summary>
    /// Live MJPEG feed, consumable straight from an &lt;img src&gt; in the browser.
    /// </summary>
    [HttpGet("{cameraId}")]
    public async Task GetStream(string cameraId, CancellationToken cancellationToken)
    {
        if (!_cameras.TryGet(cameraId, out var camera))
        {
            Response.StatusCode = StatusCodes.Status404NotFound;

            await Response.WriteAsJsonAsync(
                new { message = $"Unknown camera '{cameraId}'." },
                cancellationToken
            );

            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = $"multipart/x-mixed-replace; boundary={MultipartBoundary}";
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers.Pragma = "no-cache";

        // Without this an nginx reverse proxy buffers the response and the browser
        // receives nothing until the stream ends, which for a live feed is never.
        Response.Headers["X-Accel-Buffering"] = "no";

        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        var lastSequence = 0L;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var frame = await camera.ReadFrameAsync(
                    lastSequence,
                    FrameWaitTimeout,
                    cancellationToken
                );

                if (frame is null)
                {
                    // Ending the response lets the <img> report an error, which the
                    // frontend turns into a "Signal lost" tile and a later retry.
                    if (!camera.IsOnline)
                    {
                        break;
                    }

                    continue;
                }

                lastSequence = frame.Sequence;

                var header = Encoding.ASCII.GetBytes(
                    $"--{MultipartBoundary}\r\n" +
                    "Content-Type: image/jpeg\r\n" +
                    $"Content-Length: {frame.Data.Length}\r\n\r\n"
                );

                await Response.Body.WriteAsync(header, cancellationToken);
                await Response.Body.WriteAsync(frame.Data, cancellationToken);
                await Response.Body.WriteAsync(PartSeparator, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Viewer navigated away or closed the tab.
        }
        catch (IOException)
        {
            // Viewer dropped the connection mid-write.
        }
    }

    /// <summary>
    /// Single most recent frame. Useful as a fallback for clients that cannot hold an
    /// MJPEG connection open, and for checking a camera with curl.
    /// </summary>
    [HttpGet("{cameraId}/snapshot")]
    public IActionResult GetSnapshot(string cameraId)
    {
        if (!_cameras.TryGet(cameraId, out var camera))
        {
            return NotFound(new { message = $"Unknown camera '{cameraId}'." });
        }

        var frame = camera.Latest;

        if (frame is null)
        {
            return NotFound(new { message = $"Camera '{cameraId}' has not delivered a frame yet." });
        }

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";

        return File(frame.Data, "image/jpeg");
    }

    /// <summary>
    /// Frame ingest for the rover relay: a raw JPEG body, one frame per request.
    /// </summary>
    [HttpPost("{cameraId}/frame")]
    public async Task<IActionResult> PushFrame(string cameraId, CancellationToken cancellationToken)
    {
        if (!_cameras.TryGet(cameraId, out var camera))
        {
            return NotFound(new { message = $"Unknown camera '{cameraId}'." });
        }

        if (Request.ContentLength > _options.MaxFrameBytes)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                new { message = $"Frame exceeds the {_options.MaxFrameBytes} byte limit." }
            );
        }

        using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length > _options.MaxFrameBytes)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                new { message = $"Frame exceeds the {_options.MaxFrameBytes} byte limit." }
            );
        }

        var frame = buffer.ToArray();

        if (!LooksLikeJpeg(frame))
        {
            _logger.LogWarning(
                "Camera {CameraId}: rejected a {ByteCount} byte push that is not a JPEG.",
                camera.Id,
                frame.Length
            );

            return BadRequest(new { message = "Body must be a JPEG image." });
        }

        camera.Publish(frame);

        return Ok(new { cameraId = camera.Id, bytes = frame.Length });
    }

    private static bool LooksLikeJpeg(ReadOnlySpan<byte> data)
    {
        return data.Length > 3 && data[0] == 0xFF && data[1] == 0xD8;
    }
}