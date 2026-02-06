using Microsoft.AspNetCore.Authorization;
using System.Globalization;

namespace Kursserver.Endpoints
{
    public static class UtillityEndpoints
    {
        public static void MapUtilityEndpoints(this WebApplication app)
        {
            app.MapGet("/api/get-week/{date}/{count}", [Authorize] async (string date, int count) =>
            {
                int weeknumber;
                CultureInfo culture = new CultureInfo("sv-SE");
                DateTime WeekToDisplay;
                Calendar calendar = culture.Calendar;
                string Weekanddate;

                if (DateTime.TryParse(date, out WeekToDisplay))
                {

                    int diff = ((7 * count) + (WeekToDisplay.DayOfWeek - DayOfWeek.Monday)) % (7 * count);
                    weeknumber = calendar.GetWeekOfYear(WeekToDisplay, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
                    if (count == 1) Weekanddate = $"{WeekToDisplay.Year} - Vecka {weeknumber}: {WeekToDisplay.AddDays(-diff).Date.ToString("d/M", CultureInfo.InvariantCulture)} - {WeekToDisplay.AddDays(-diff + 6).Date.ToString("d/M", CultureInfo.InvariantCulture)}";
                    else Weekanddate = $"{WeekToDisplay.Year} - Vecka {weeknumber - 1} & {weeknumber}: {WeekToDisplay.AddDays(-diff - 7).Date.ToString("d/M", CultureInfo.InvariantCulture)} - {WeekToDisplay.AddDays(-diff + 6).Date.ToString("d/M", CultureInfo.InvariantCulture)}";
                    return Results.Ok(Weekanddate);
                }
                else return Results.Problem("Felaktigt datumformat");
            });
        }
    }
}
