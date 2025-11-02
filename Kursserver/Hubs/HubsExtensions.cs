using Microsoft.AspNetCore.Authorization;

namespace Kursserver.Hubs
{
    public static class HubsExtensions
    {
        public static void MapWebsocketEndpoints(this WebApplication app)
        {

            app.MapHub<ChatHub>("/chatHub"); 
            app.MapHub<NotificationHub>("notificationHub");
        }
    }
}
