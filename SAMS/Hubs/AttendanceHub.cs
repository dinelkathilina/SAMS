namespace SAMS.Hubs;
using Microsoft.AspNetCore.SignalR;

public class AttendanceHub : Hub
{
    public async Task JoinSession(string sessionCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionCode);
    }

    public async Task LeaveSession(string sessionCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionCode);
    }
}