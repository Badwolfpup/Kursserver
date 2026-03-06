using FluentAssertions;
using Kursserver.Models;
using Kursserver.Utils;

namespace Kursserver.Tests.Utils;

public class RecurringEventExpanderTests
{
    private static RecurringEvent MakeEvent(
        int id = 1,
        DayOfWeek weekday = DayOfWeek.Monday,
        int startHour = 10,
        int endHour = 11,
        string frequency = "weekly",
        DateTime? startDate = null,
        int adminId = 1)
    {
        return new RecurringEvent
        {
            Id = id,
            Name = $"Event {id}",
            Weekday = weekday,
            StartTime = TimeSpan.FromHours(startHour),
            EndTime = TimeSpan.FromHours(endHour),
            Frequency = frequency,
            StartDate = startDate ?? new DateTime(2026, 1, 1),
            AdminId = adminId,
            CreatedAt = DateTime.Now
        };
    }

    // --- Weekly expansion ---

    [Fact]
    public void ExpandOccurrences_Weekly_ReturnsOnePerWeek()
    {
        var ev = MakeEvent(weekday: DayOfWeek.Monday);
        var from = new DateTime(2026, 3, 2); // Monday
        var to = new DateTime(2026, 3, 22); // 3 weeks

        var result = RecurringEventExpander.ExpandOccurrences(ev, from, to, [], []);

        result.Should().HaveCount(3);
        result[0].Date.Should().Be(new DateTime(2026, 3, 2));
        result[1].Date.Should().Be(new DateTime(2026, 3, 9));
        result[2].Date.Should().Be(new DateTime(2026, 3, 16));
    }

    [Fact]
    public void ExpandOccurrences_Weekly_SetsCorrectTimes()
    {
        var ev = MakeEvent(weekday: DayOfWeek.Monday, startHour: 10, endHour: 11);
        var from = new DateTime(2026, 3, 2);
        var to = new DateTime(2026, 3, 2);

        var result = RecurringEventExpander.ExpandOccurrences(ev, from, to, [], []);

        result.Should().HaveCount(1);
        result[0].Start.Should().Be(new DateTime(2026, 3, 2, 10, 0, 0));
        result[0].End.Should().Be(new DateTime(2026, 3, 2, 11, 0, 0));
    }

    // --- Biweekly expansion ---

    [Fact]
    public void ExpandOccurrences_Biweekly_ReturnsEveryOtherWeek()
    {
        var ev = MakeEvent(weekday: DayOfWeek.Monday, frequency: "biweekly", startDate: new DateTime(2026, 3, 2));
        var from = new DateTime(2026, 3, 2);
        var to = new DateTime(2026, 3, 29); // just under 4 weeks — excludes March 30

        var result = RecurringEventExpander.ExpandOccurrences(ev, from, to, [], []);

        result.Should().HaveCount(2);
        result[0].Date.Should().Be(new DateTime(2026, 3, 2));
        result[1].Date.Should().Be(new DateTime(2026, 3, 16));
    }

    // --- Weekday alignment ---

    [Fact]
    public void ExpandOccurrences_AlignsToCorrectWeekday()
    {
        // Event is on Wednesday, startDate is Monday
        var ev = MakeEvent(weekday: DayOfWeek.Wednesday, startDate: new DateTime(2026, 3, 2)); // Monday
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 8);

        var result = RecurringEventExpander.ExpandOccurrences(ev, from, to, [], []);

        result.Should().HaveCount(1);
        result[0].Date.DayOfWeek.Should().Be(DayOfWeek.Wednesday);
        result[0].Date.Should().Be(new DateTime(2026, 3, 4));
    }

    // --- NoClass days ---

    [Fact]
    public void ExpandOccurrences_SkipsNoClassDays()
    {
        var ev = MakeEvent(weekday: DayOfWeek.Monday);
        var from = new DateTime(2026, 3, 2);
        var to = new DateTime(2026, 3, 16);
        var noClassDates = new List<DateTime> { new DateTime(2026, 3, 9) };

        var result = RecurringEventExpander.ExpandOccurrences(ev, from, to, [], noClassDates);

        result.Should().HaveCount(2);
        result.Should().NotContain(i => i.Date == new DateTime(2026, 3, 9));
    }

    // --- Exceptions ---

    [Fact]
    public void ExpandOccurrences_DeletedExceptionSkipsOccurrence()
    {
        var ev = MakeEvent(weekday: DayOfWeek.Monday);
        var from = new DateTime(2026, 3, 2);
        var to = new DateTime(2026, 3, 16);
        var exceptions = new List<RecurringEventException>
        {
            new() { RecurringEventId = 1, Date = new DateTime(2026, 3, 9), IsDeleted = true }
        };

        var result = RecurringEventExpander.ExpandOccurrences(ev, from, to, exceptions, []);

        result.Should().HaveCount(2);
        result.Should().NotContain(i => i.Date == new DateTime(2026, 3, 9));
    }

    [Fact]
    public void ExpandOccurrences_ExceptionOverridesName()
    {
        var ev = MakeEvent(weekday: DayOfWeek.Monday);
        var from = new DateTime(2026, 3, 9);
        var to = new DateTime(2026, 3, 9);
        var exceptions = new List<RecurringEventException>
        {
            new() { RecurringEventId = 1, Date = new DateTime(2026, 3, 9), IsDeleted = false, Name = "Special" }
        };

        var result = RecurringEventExpander.ExpandOccurrences(ev, from, to, exceptions, []);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Special");
        result[0].IsException.Should().BeTrue();
    }

    [Fact]
    public void ExpandOccurrences_ExceptionOverridesTime()
    {
        var ev = MakeEvent(weekday: DayOfWeek.Monday, startHour: 10, endHour: 11);
        var from = new DateTime(2026, 3, 9);
        var to = new DateTime(2026, 3, 9);
        var exceptions = new List<RecurringEventException>
        {
            new()
            {
                RecurringEventId = 1, Date = new DateTime(2026, 3, 9), IsDeleted = false,
                StartTime = TimeSpan.FromHours(14), EndTime = TimeSpan.FromHours(15)
            }
        };

        var result = RecurringEventExpander.ExpandOccurrences(ev, from, to, exceptions, []);

        result.Should().HaveCount(1);
        result[0].Start.Hour.Should().Be(14);
        result[0].End.Hour.Should().Be(15);
    }

    [Fact]
    public void ExpandOccurrences_ExceptionForDifferentEvent_IsIgnored()
    {
        var ev = MakeEvent(id: 1, weekday: DayOfWeek.Monday);
        var from = new DateTime(2026, 3, 9);
        var to = new DateTime(2026, 3, 9);
        var exceptions = new List<RecurringEventException>
        {
            new() { RecurringEventId = 999, Date = new DateTime(2026, 3, 9), IsDeleted = true }
        };

        var result = RecurringEventExpander.ExpandOccurrences(ev, from, to, exceptions, []);

        result.Should().HaveCount(1);
    }

    // --- Range filtering ---

    [Fact]
    public void ExpandOccurrences_ExcludesOccurrencesBeforeRange()
    {
        var ev = MakeEvent(weekday: DayOfWeek.Monday, startDate: new DateTime(2026, 1, 1));
        var from = new DateTime(2026, 3, 9);
        var to = new DateTime(2026, 3, 9);

        var result = RecurringEventExpander.ExpandOccurrences(ev, from, to, [], []);

        result.Should().HaveCount(1);
        result[0].Date.Should().Be(new DateTime(2026, 3, 9));
    }

    [Fact]
    public void ExpandOccurrences_EmptyRangeReturnsEmpty()
    {
        var ev = MakeEvent(weekday: DayOfWeek.Monday);
        // Range that contains no Monday
        var from = new DateTime(2026, 3, 3); // Tuesday
        var to = new DateTime(2026, 3, 6);   // Friday

        var result = RecurringEventExpander.ExpandOccurrences(ev, from, to, [], []);

        result.Should().BeEmpty();
    }

    // --- ExpandAll ---

    [Fact]
    public void ExpandAll_CombinesMultipleEventsOrderedByStart()
    {
        var ev1 = MakeEvent(id: 1, weekday: DayOfWeek.Monday, startHour: 10, endHour: 11);
        var ev2 = MakeEvent(id: 2, weekday: DayOfWeek.Monday, startHour: 8, endHour: 9, adminId: 2);
        var from = new DateTime(2026, 3, 2);
        var to = new DateTime(2026, 3, 2);

        var result = RecurringEventExpander.ExpandAll([ev1, ev2], from, to, [], []);

        result.Should().HaveCount(2);
        result[0].Start.Hour.Should().Be(8); // ev2 first (earlier)
        result[1].Start.Hour.Should().Be(10); // ev1 second
    }
}
