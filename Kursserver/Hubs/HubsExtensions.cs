using Microsoft.AspNetCore.Authorization;

namespace Kursserver.Hubs
{
    public static class HubsExtensions
    {
        public static void MapWebsocketEndpoints(this WebApplication app)
        {
            app.MapHub<NewsHub>("/newsHub"); 
            app.MapHub<ChatHub>("/chatHub"); 
            app.MapHub<ExerciseHub>("/exerciseHub");  
            app.MapHub<CourseHub>("/courseHub"); 
            app.MapHub<SettingsHub>("/settingsHub");  
            app.MapHub<AdminHub>("/adminHub");  
            app.MapHub<NotificationHub>("notificationHub");
        }
    }
}
