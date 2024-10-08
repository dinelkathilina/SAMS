using Microsoft.AspNetCore.SignalR;

namespace SAMS.Hubs
{
    public class SessionHub :Hub
    {
        public async Task SendSessionUpdate(string message)
        {
            await Clients.All.SendAsync("ReceiveSessionUpdate", message);
        }
    }
}
