using Kursserver.Models;

namespace Kursserver.Utils
{
    public record RecurringEventInstance(
        int EventId,
        string Name,
        DateTime Start,
        DateTime End,
        DateTime Date,
        int AdminId,
        string Frequency,
        bool IsException,
        int? Classroom);

    public static class RecurringEventExpander
    {
        public static List<RecurringEventInstance> ExpandOccurrences(
            RecurringEvent ev,
            DateTime rangeStart,
            DateTime rangeEnd,
            List<RecurringEventException> allExceptions,
            List<DateTime> noClassDates)
        {
            var instances = new List<RecurringEventInstance>();
            var exceptions = allExceptions
                .Where(e => e.RecurringEventId == ev.Id)
                .ToList();

            var current = ev.StartDate.Date;
            // Align to the correct weekday
            while (current.DayOfWeek != ev.Weekday)
                current = current.AddDays(1);

            var increment = ev.Frequency == "biweekly" ? 14 : 7;

            while (current <= rangeEnd.Date)
            {
                if (current >= rangeStart.Date)
                {
                    // Skip NoClass days
                    if (noClassDates.Contains(current))
                    {
                        current = current.AddDays(increment);
                        continue;
                    }

                    var exception = exceptions.FirstOrDefault(e => e.Date.Date == current);

                    if (exception != null && exception.IsDeleted)
                    {
                        current = current.AddDays(increment);
                        continue;
                    }

                    var name = exception?.Name ?? ev.Name;
                    var startTime = exception?.StartTime ?? ev.StartTime;
                    var endTime = exception?.EndTime ?? ev.EndTime;
                    var classroom = exception?.Classroom ?? ev.Classroom;

                    instances.Add(new RecurringEventInstance(
                        EventId: ev.Id,
                        Name: name,
                        Start: current + startTime,
                        End: current + endTime,
                        Date: current,
                        AdminId: ev.AdminId,
                        Frequency: ev.Frequency,
                        IsException: exception != null,
                        Classroom: classroom));
                }

                current = current.AddDays(increment);
            }

            return instances;
        }

        public static List<RecurringEventInstance> ExpandAll(
            List<RecurringEvent> events,
            DateTime rangeStart,
            DateTime rangeEnd,
            List<RecurringEventException> exceptions,
            List<DateTime> noClassDates)
        {
            return events
                .SelectMany(ev => ExpandOccurrences(ev, rangeStart, rangeEnd, exceptions, noClassDates))
                .OrderBy(i => i.Start)
                .ToList();
        }
    }
}
