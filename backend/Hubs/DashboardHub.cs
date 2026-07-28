using backend.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs;

public class DashboardHub : Hub
{
    public async Task RegisterRobot(string roverId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"rover-{roverId}");

        await Clients.Caller.SendAsync("RobotRegistered", new
        {
            roverId = roverId,
            message = "Robot registered to command channel."
        });
    }

    public async Task SendCommandToRobot(SendCommandDto dto)
    {
        await Clients.Group($"rover-{dto.RoverId}").SendAsync("ReceiveCommand", dto);
    }
}