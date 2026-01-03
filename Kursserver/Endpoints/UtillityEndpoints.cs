using System.Globalization;

namespace Kursserver.Endpoints
{
    public static class UtillityEndpoints
    {
        public static void MapUtilityEndpoints(this WebApplication app)
        {
            app.MapGet("/api/get-week/{date}", async (string date) =>
            {
                int weeknumber;
                CultureInfo culture = new CultureInfo("sv-SE");
                DateTime WeekToDisplay;
                Calendar calendar = culture.Calendar;
                string Weekanddate;

                if (DateTime.TryParse(date, out WeekToDisplay))
                {

                    int diff = (7 + (WeekToDisplay.DayOfWeek - DayOfWeek.Monday)) % 7;
                    weeknumber = calendar.GetWeekOfYear(WeekToDisplay, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
                    Weekanddate = $"{WeekToDisplay.Year} - Vecka {weeknumber}: {WeekToDisplay.AddDays(-diff).Date.ToString("d/M", CultureInfo.InvariantCulture)} - {WeekToDisplay.AddDays(-diff + 6).Date.ToString("d/M", CultureInfo.InvariantCulture)}";
                    return Results.Ok(Weekanddate);
                }
                else return Results.Problem("Felaktigt datumformat");
            });
        }
    }
}
