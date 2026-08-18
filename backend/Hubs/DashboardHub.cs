using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs;

public class DashboardHub : Hub
{
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

        await Clients.Caller.SendAsync("RobotRegistered", new
        {
            roverId,
            message = "Robot registered to command channel."
        });
    }
}