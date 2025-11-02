using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Kursserver.Hubs
{
    [Authorize]
    public class ConnctionHandler: Hub
    {
        public static class ConnectionHandler
        {
            public static async Task HandleConnectionAsync(HubCallerContext context, IHubCallerClients clients, IGroupManager groups)
            {
                var userName = context.User?.Identity?.Name ?? "Anonymous";
                await clients.Caller.SendAsync("ReceiveMessage", "System", $"Welcome, {userName}! You are connected.");

                           }

            public static async Task HandleDisconnectionAsync(HubCallerContext context, IHubCallerClients clients, IGroupManager groups)
            {
                var userName = context.User?.Identity?.Name ?? "Anonymous";
                await clients.Caller.SendAsync("ReceiveMessage", "System", $"You have disconnected.");
            }
        }
    }
}
