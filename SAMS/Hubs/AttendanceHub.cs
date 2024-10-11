using Microsoft.AspNetCore.SignalR;

namespace SAMS.Hubs
{
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

        public async Task UpdateSessionStatus(string sessionCode, string status)
        {
            await Clients.Group(sessionCode).SendAsync("SessionStatusUpdated", status);
        }
    }
}