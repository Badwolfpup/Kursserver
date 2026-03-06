using FluentAssertions;
using Kursserver.Utils;

namespace Kursserver.Tests.Helpers;

public class ScheduleHelpersTests
{
    private static DateTime T(int hour) => new DateTime(2025, 1, 1, hour, 0, 0);

    // --- Overlaps ---

    [Fact]
    public void Overlaps_WhenRangesOverlap_ReturnsTrue()
    {
        ScheduleHelpers.Overlaps(T(9), T(11), T(10), T(12)).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WhenAdjacentEndToStart_ReturnsFalse()
    {
        ScheduleHelpers.Overlaps(T(9), T(10), T(10), T(11)).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_WhenNoOverlap_ReturnsFalse()
    {
        ScheduleHelpers.Overlaps(T(9), T(10), T(11), T(12)).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_WhenOneContainsOther_ReturnsTrue()
    {
        ScheduleHelpers.Overlaps(T(9), T(13), T(10), T(12)).Should().BeTrue();
    }

    // --- IsFullyBooked ---

    [Fact]
    public void IsFullyBooked_NoBookings_ReturnsFalse()
    {
        ScheduleHelpers.IsFullyBooked(T(9), T(11), []).Should().BeFalse();
    }

    [Fact]
    public void IsFullyBooked_SingleBookingCoveringAll_ReturnsTrue()
    {
        var bookings = new[] { (T(9), T(11)) };
        ScheduleHelpers.IsFullyBooked(T(9), T(11), bookings).Should().BeTrue();
    }

    [Fact]
    public void IsFullyBooked_TwoBookingsCoveringAll_ReturnsTrue()
    {
        var bookings = new[] { (T(9), T(10)), (T(10), T(11)) };
        ScheduleHelpers.IsFullyBooked(T(9), T(11), bookings).Should().BeTrue();
    }

    [Fact]
    public void IsFullyBooked_GapInMiddle_ReturnsFalse()
    {
        var bookings = new[] { (T(9), T(10)), (T(10).AddMinutes(30), T(11)) };
        ScheduleHelpers.IsFullyBooked(T(9), T(11), bookings).Should().BeFalse();
    }

    [Fact]
    public void IsFullyBooked_PartialCoverageAtEnd_ReturnsFalse()
    {
        var bookings = new[] { (T(9), T(10)) };
        ScheduleHelpers.IsFullyBooked(T(9), T(11), bookings).Should().BeFalse();
    }

    [Fact]
    public void IsFullyBooked_BookingsOutOfOrder_ReturnsTrue()
    {
        var bookings = new[] { (T(10), T(11)), (T(9), T(10)) };
        ScheduleHelpers.IsFullyBooked(T(9), T(11), bookings).Should().BeTrue();
    }

    [Fact]
    public void IsFullyBooked_OverlappingBookingsCoveringAll_ReturnsTrue()
    {
        // First booking runs 9-10:30, second starts at 10 — overlapping coverage
        var bookings = new[] { (T(9), T(10).AddMinutes(30)), (T(10), T(11)) };
        ScheduleHelpers.IsFullyBooked(T(9), T(11), bookings).Should().BeTrue();
    }

    [Fact]
    public void IsFullyBooked_BookingStartingBeforeWindow_ReturnsTrue()
    {
        // Booking starts an hour early but covers the full window
        var bookings = new[] { (T(8), T(11)) };
        ScheduleHelpers.IsFullyBooked(T(9), T(11), bookings).Should().BeTrue();
    }

    [Fact]
    public void IsFullyBooked_BookingStartingBeforeWindowLeavesGap_ReturnsFalse()
    {
        // Booking starts early but only reaches T(10), leaving T(10)-T(11) uncovered
        var bookings = new[] { (T(8), T(10)) };
        ScheduleHelpers.IsFullyBooked(T(9), T(11), bookings).Should().BeFalse();
    }
}
