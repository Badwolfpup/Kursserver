using Kursserver.Extracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Globalization;

namespace Kursserver.Utils
{
    [Authorize]
    public static class GetWeek
    {

        public static void GetWeekEndpints(this WebApplication app)
        {
            int weeknumber;
            CultureInfo culture = new CultureInfo("sv-SE");
            DateTime WeekToDisplay;
            Calendar calendar = culture.Calendar;
            string Weekanddate;
            

            app.MapPost("/api/get-week", async (HttpContext context) =>
            {
                using (var reader = new StreamReader(context.Request.Body))
                {
                    string request = await reader.ReadToEndAsync();
                    if (request == "")
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync(new { error = "No date was provided." });
                        return;
                    }
                    if (DateTime.TryParse(request, out WeekToDisplay))
                    {

                        int diff = (7 + (WeekToDisplay.DayOfWeek - DayOfWeek.Monday)) % 7;
                        weeknumber = calendar.GetWeekOfYear(WeekToDisplay, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
                        Weekanddate = $"{WeekToDisplay.Year} - Vecka {weeknumber}: {WeekToDisplay.AddDays(-diff).Date.ToString("d/M", CultureInfo.InvariantCulture)} - {WeekToDisplay.AddDays(-diff + 6).Date.ToString("d/M", CultureInfo.InvariantCulture)}";
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsJsonAsync(Weekanddate);
                    }
                }
            });

                
        }
    }
}
