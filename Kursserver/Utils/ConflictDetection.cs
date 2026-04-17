using Kursserver.Models;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Utils
{
    public static class ConflictDetection
    {
        /// <summary>
        /// Returns accepted bookings that overlap the given time range for the given admin or coach.
        /// Pending bookings are intentionally ignored — multiple pending suggestions may coexist;
        /// only an accepted booking is a hard conflict.
        /// </summary>
        public static async Task<List<Booking>> CheckAcceptedBookingConflicts(
            ApplicationDbContext db, DateTime startTime, DateTime endTime,
            int? adminId = null, int? coachId = null,
            int? excludeBookingId = null)
        {
            var query = db.Bookings
                .Where(b => b.Status == BookingStatus.Accepted
                         && b.StartTime < endTime
                         && b.EndTime > startTime);

            if (excludeBookingId.HasValue)
                query = query.Where(b => b.Id != excludeBookingId.Value);

            var conflicts = await query.ToListAsync();

            return conflicts
                .Where(b =>
                    (adminId.HasValue && b.AdminId == adminId.Value) ||
                    (coachId.HasValue && b.CoachId == coachId.Value))
                .DistinctBy(b => b.Id)
                .ToList();
        }
    }
}
