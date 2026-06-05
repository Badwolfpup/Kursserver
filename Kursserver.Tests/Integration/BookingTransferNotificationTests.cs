using System.Net.Http.Json;
using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Kursserver.Tests.Integration;

/// <summary>
/// Regression tests for the bug where accepting a booking on a colleague's behalf
/// (a transfer of a pending booking) notified the coach but never the newly-assigned
/// teacher. See BookingNotifier.NotifyTransferred.
/// </summary>
public class BookingTransferNotificationTests : IntegrationTestBase
{
    public BookingTransferNotificationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private FakeEmailService Emails => (FakeEmailService)Factory.Services.GetRequiredService<IEmailService>();

    private async Task<(int BookingId, int NewAdminId, string CoachEmail, string NewAdminEmail)> SeedPendingBookingAsync(bool newAdminWantsEmail)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // The factory (IClassFixture) is shared across tests in this class, so the
        // in-memory DB and the fake's captured list persist — reset both per test.
        db.Bookings.RemoveRange(db.Bookings);
        await db.SaveChangesAsync();
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();
        Emails.Clear();

        var coach = new User { FirstName = "Cora", LastName = "Coach", Email = "cora.coach@test.se", AuthLevel = Role.Coach };
        var oldAdmin = new User { FirstName = "Olle", LastName = "Original", Email = "olle.original@test.se", AuthLevel = Role.Admin };
        var newAdmin = new User { FirstName = "Nora", LastName = "Newteacher", Email = "nora.new@test.se", AuthLevel = Role.Teacher, EmailNotifications = newAdminWantsEmail };
        db.Users.AddRange(coach, oldAdmin, newAdmin);
        await db.SaveChangesAsync();

        var booking = new Booking
        {
            AdminId = oldAdmin.Id,
            CoachId = coach.Id,
            StartTime = new DateTime(2026, 6, 10, 14, 0, 0),
            EndTime = new DateTime(2026, 6, 10, 14, 30, 0),
            MeetingType = MeetingType.Intro,
            Status = BookingStatus.Pending,
            CreatedByRole = BookingActor.Coach,
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        return (booking.Id, newAdmin.Id, coach.Email, newAdmin.Email);
    }

    [Fact]
    public async Task Transfer_NotifiesNewlyAssignedTeacher_WhenEmailNotificationsEnabled()
    {
        var seed = await SeedPendingBookingAsync(newAdminWantsEmail: true);

        var client = CreateClient();
        var response = await client.PutAsJsonAsync(
            $"/api/bookings/{seed.BookingId}/transfer",
            new TransferBookingDto { TargetAdminId = seed.NewAdminId });

        response.EnsureSuccessStatusCode();

        var sent = Emails.Sent;
        sent.Should().Contain(
            e => e.To == seed.NewAdminEmail && e.Subject == "Möte tilldelat dig",
            "the teacher the meeting was assigned to should be notified");
        sent.Should().Contain(
            e => e.To == seed.CoachEmail && e.Subject == "Ändring av mötesdeltagare",
            "the coach should still be told the participating teacher changed");
    }

    [Fact]
    public async Task Transfer_DoesNotNotifyNewTeacher_WhenEmailNotificationsDisabled()
    {
        var seed = await SeedPendingBookingAsync(newAdminWantsEmail: false);

        var client = CreateClient();
        var response = await client.PutAsJsonAsync(
            $"/api/bookings/{seed.BookingId}/transfer",
            new TransferBookingDto { TargetAdminId = seed.NewAdminId });

        response.EnsureSuccessStatusCode();

        var sent = Emails.Sent;
        sent.Should().NotContain(
            e => e.To == seed.NewAdminEmail,
            "a teacher who disabled email notifications should not receive the assignment email");
        sent.Should().Contain(
            e => e.To == seed.CoachEmail && e.Subject == "Ändring av mötesdeltagare",
            "the coach is notified of the participant change regardless of the teacher's preference");
    }
}
