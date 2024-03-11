using Microsoft.AspNetCore.SignalR;

namespace App.Core.Hubs
{
    public class AutomationHub : Hub
    {
        public async Task SendLog(string username, string entity, string log)
        {
            await Clients.All.SendAsync("AutomationLogs", username, entity, log);
        }
    }
}
