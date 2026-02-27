namespace Kursserver.Utils
{
    public static class ScheduleHelpers
    {
        public static bool Overlaps(DateTime s1, DateTime e1, DateTime s2, DateTime e2)
            => s1 < e2 && e1 > s2;

        public static bool IsFullyBooked(DateTime availStart, DateTime availEnd,
            IEnumerable<(DateTime StartTime, DateTime EndTime)> bookings)
        {
            var coveredStart = availStart;
            foreach (var b in bookings.OrderBy(b => b.StartTime))
            {
                if (b.StartTime > coveredStart) return false;
                if (b.EndTime > coveredStart) coveredStart = b.EndTime;
            }
            return coveredStart >= availEnd;
        }
    }
}
