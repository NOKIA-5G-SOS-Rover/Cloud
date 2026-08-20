using backend.Extensions;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/rover-control")]
public class RoverControlController : ControllerBase
{
    private readonly RoverControlService
        _roverControlService;

    public RoverControlController(
        RoverControlService roverControlService)
    {
        _roverControlService =
            roverControlService;
    }


    [HttpPost("take")]
    public async Task<IActionResult>
        TakeControl()
    {
        var user =
            HttpContext.GetCurrentUser();

        if (user == null)
            return Unauthorized();

        var result =
            await _roverControlService
                .TakeControlAsync(user);

        if (!result.Success)
        {
            return StatusCode(403, new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message
        });
    }


    [HttpPost("release")]
    public async Task<IActionResult>
        ReleaseControl()
    {
        var user =
            HttpContext.GetCurrentUser();

        if (user == null)
            return Unauthorized();

        var released =
            await _roverControlService
                .ReleaseControlAsync(user);

        if (!released)
        {
            return BadRequest(new
            {
                message =
                    "You do not control the rover."
            });
        }

        return Ok(new
        {
            message =
                "Rover control released."
        });
    }


    [HttpGet("status")]
    public async Task<IActionResult>
        GetStatus()
    {
        var user =
            HttpContext.GetCurrentUser();

        if (user == null)
            return Unauthorized();

        var control =
            await _roverControlService
                .GetActiveControlAsync();

        if (control == null)
        {
            return Ok(new
            {
                isControlled = false
            });
        }

        var controlTime =
            DateTime.UtcNow -
            control.StartedAt;

        return Ok(new
        {
            isControlled = true,

            controlledBy = new
            {
                id = control.User.Id,
                username =
                    control.User.Username
            },

            startedAt =
                control.StartedAt,

            controlTimeSeconds =
                (long)controlTime
                    .TotalSeconds,

            lastActivityAt =
                control.LastActivityAt
        });
    }
}