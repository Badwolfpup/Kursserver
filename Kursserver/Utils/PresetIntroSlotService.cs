using Kursserver.Models;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Utils
{
    // SCENARIO: On startup and every Sunday, ensures preset intro slots exist for the next 26 weeks.
    // Victoria (admin, Tuesday 10-11) and Adam (admin, Thursday 11-12) get one AdminAvailability
    // per week if none already exists in that time window.
    public class PresetIntroSlotService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PresetIntroSlotService> _logger;

        private record PresetConfig(string FirstName, DayOfWeek Day, int StartHour, int EndHour);

        private static readonly PresetConfig[] Presets =
        [
            new("Victoria", DayOfWeek.Tuesday, 10, 11),
            new("Adam",     DayOfWeek.Thursday, 11, 12),
        ];

        public PresetIntroSlotService(IServiceScopeFactory scopeFactory, ILogger<PresetIntroSlotService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Seed all 26 weeks on startup
            await GenerateSlotsAsync(stoppingToken);

            // Then run weekly on Sunday
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextSunday = GetNextSunday(now);
                var delay = nextSunday - now;
                _logger.LogInformation("PresetIntroSlotService: next run in {Hours:F1}h (next Sunday {Date})", delay.TotalHours, nextSunday);

                try { await Task.Delay(delay, stoppingToken); }
                catch (OperationCanceledException) { break; }

                await GenerateSlotsAsync(stoppingToken);
            }
        }

        private static DateTime GetNextSunday(DateTime from)
        {
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)from.DayOfWeek + 7) % 7;
            if (daysUntilSunday == 0) daysUntilSunday = 7; // if today is Sunday, wait a full week
            return from.Date.AddDays(daysUntilSunday).AddHours(1); // 01:00 Sunday morning
        }

        private async Task GenerateSlotsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.Today;

            foreach (var preset in Presets)
            {
                // Find admin by first name (AuthLevel 1 or 2, active)
                var admin = await db.Users
                    .Where(u => u.FirstName == preset.FirstName && (u.AuthLevel == Role.Admin || u.AuthLevel == Role.Teacher) && u.IsActive)
                    .FirstOrDefaultAsync(ct);

                if (admin == null)
                {
                    _logger.LogWarning("PresetIntroSlotService: no active admin found with first name '{Name}'", preset.FirstName);
                    continue;
                }

                // Generate slots for the next 26 weeks
                for (int week = 0; week < 26; week++)
                {
                    var slotDate = GetNextOccurrence(today.AddDays(week * 7), preset.Day);
                    var slotStart = slotDate.AddHours(preset.StartHour);
                    var slotEnd = slotDate.AddHours(preset.EndHour);

                    // Check if overlapping slot already exists for this admin on this date/time
                    var exists = await db.AdminAvailabilities.AnyAsync(a =>
                        a.AdminId == admin.Id &&
                        a.StartTime < slotEnd &&
                        a.EndTime > slotStart,
                        ct);

                    if (!exists)
                    {
                        db.AdminAvailabilities.Add(new AdminAvailability
                        {
                            AdminId = admin.Id,
                            StartTime = slotStart,
                            EndTime = slotEnd,
                            IsBooked = false,
                        });
                        _logger.LogInformation("PresetIntroSlotService: created slot for {Name} on {Date}", preset.FirstName, slotStart);
                    }
                }
            }

            await db.SaveChangesAsync(ct);
        }

        // Returns the date of the next occurrence of dayOfWeek on or after startDate
        private static DateTime GetNextOccurrence(DateTime startDate, DayOfWeek dayOfWeek)
        {
            int daysUntil = ((int)dayOfWeek - (int)startDate.DayOfWeek + 7) % 7;
            return startDate.Date.AddDays(daysUntil);
        }
    }
}
