using Kursserver.Models;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Utils
{
    public record PresetIntroRule(string FirstName, DayOfWeek Day, int StartHour, int EndHour);

    public static class PresetIntroConfig
    {
        public static readonly PresetIntroRule[] Rules =
        [
            new("Victoria", DayOfWeek.Tuesday, 10, 11),
            new("Adam",     DayOfWeek.Thursday, 11, 12),
        ];

        /// <summary>
        /// Checks whether a time range overlaps with a preset intro window for the given admin.
        /// Returns true if the admin has a preset and the booking overlaps it.
        /// </summary>
        public static async Task<bool> OverlapsPresetIntro(
            int adminId, DateTime start, DateTime end, ApplicationDbContext db)
        {
            foreach (var rule in Rules)
            {
                // Resolve admin by first name
                var admin = await db.Users
                    .Where(u => u.FirstName == rule.FirstName
                        && (u.AuthLevel == Role.Admin || u.AuthLevel == Role.Teacher)
                        && u.IsActive)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                if (admin == 0 || admin != adminId) continue;

                // Check if the booking's day matches and time overlaps the preset window
                if (start.DayOfWeek == rule.Day)
                {
                    var presetStart = start.Date.AddHours(rule.StartHour);
                    var presetEnd = start.Date.AddHours(rule.EndHour);
                    if (start < presetEnd && end > presetStart)
                        return true;
                }
            }

            return false;
        }
    }
}
