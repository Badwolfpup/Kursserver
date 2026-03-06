using Kursserver.Models;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Utils
{
    public static class ConflictDetection
    {
        public static async Task<List<Booking>> CheckBookingConflicts(
            ApplicationDbContext db, DateTime startTime, DateTime endTime,
            int? adminId = null, int? coachId = null, int? studentId = null,
            int? excludeBookingId = null)
        {
            var query = db.Bookings
                .Where(b => b.Status != "declined"
                         && b.StartTime < endTime
                         && b.EndTime > startTime);

            if (excludeBookingId.HasValue)
                query = query.Where(b => b.Id != excludeBookingId.Value);

            var conflicts = await query.ToListAsync();

            return conflicts
                .Where(b =>
                    (adminId.HasValue && b.AdminId == adminId.Value) ||
                    (coachId.HasValue && b.CoachId == coachId.Value) ||
                    (studentId.HasValue && b.StudentId == studentId.Value))
                .DistinctBy(b => b.Id)
                .ToList();
        }

        public static async Task<List<(DateTime Start, DateTime End, string Name)>> CheckRecurringEventConflicts(
            ApplicationDbContext db, DateTime startTime, DateTime endTime)
        {
            var events = await db.RecurringEvents.ToListAsync();
            var exceptions = await db.RecurringEventExceptions.ToListAsync();
            var noClassDates = await db.NoClasses.Select(n => n.Date.Date).ToListAsync();

            var conflicts = new List<(DateTime, DateTime, string)>();

            foreach (var ev in events)
            {
                var instances = RecurringEventExpander.ExpandOccurrences(
                    ev, startTime, endTime, exceptions, noClassDates);

                foreach (var inst in instances)
                {
                    if (inst.Start < endTime && inst.End > startTime)
                        conflicts.Add((inst.Start, inst.End, inst.Name));
                }
            }

            return conflicts;
        }
    }
}
