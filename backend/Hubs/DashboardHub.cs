using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs;

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
}