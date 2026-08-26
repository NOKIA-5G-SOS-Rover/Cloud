using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace backend.Hubs;

// We define the payload structure so C# knows how to parse the JSON from React
public class RoverCommandPayload
{
    public string RoverId { get; set; }
    public string Command { get; set; }
    public int Speed { get; set; }
    public int? Degrees { get; set; }
}

public class DashboardHub : Hub
{
    public const string DashboardGroup = "dashboards";

    public async Task RegisterRobot(string roverId)
    {
        roverId = roverId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(roverId))
        {
            throw new HubException("RoverId is required.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"rover-{roverId}"
        );

        await Clients.Caller.SendAsync(
            "RobotRegistered",
            new
            {
                roverId,
                message = "Robot registered to command channel."
            }
        );
    }

    public async Task RegisterDashboard()
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            DashboardGroup
        );

        await Clients.Caller.SendAsync(
            "DashboardRegistered",
            new
            {
                message = "Dashboard registered successfully."
            }
        );
    }

    // THE FIX: This method receives the command from React and routes it to the Rover
    public async Task SendCommandToRobot(RoverCommandPayload payload)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.RoverId))
        {
            throw new HubException("Invalid payload or missing RoverId.");
        }

        // Send the payload to the specific rover's group using the "ReceiveCommand" event
        // This exactly matches what your Python script is listening for!
        await Clients.Group($"rover-{payload.RoverId}").SendAsync("ReceiveCommand", payload);
    }
}
