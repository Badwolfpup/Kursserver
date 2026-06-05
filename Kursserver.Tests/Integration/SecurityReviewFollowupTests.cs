using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Kursserver.Tests.Integration;

/// <summary>
/// Regression guards for the 2026-06 security-review follow-up fixes:
/// - Availability overlays are owner-scoped (mirrors BusyTime).
/// - Recurring-event writes are owner-or-Admin only (the documented "Teacher = own").
/// - Absence-warning send only targets a registered coach (no arbitrary-recipient relay).
/// - GET /api/bookings is staff-only (no full dump to a Guest token).
/// </summary>
public class SecurityReviewFollowupTests : IntegrationTestBase
{
    public SecurityReviewFollowupTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task ResetDbAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.RecurringEvents.RemoveRange(db.RecurringEvents);
        db.AdminAvailabilities.RemoveRange(db.AdminAvailabilities);
        db.Bookings.RemoveRange(db.Bookings);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();
    }

    private async Task<int> AddUserAsync(Role role, string email)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var u = new User { FirstName = role.ToString(), LastName = "X", Email = email, AuthLevel = role };
        db.Users.Add(u);
        await db.SaveChangesAsync();
        return u.Id;
    }

    private async Task<int> AddRecurringEventAsync(int adminId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ev = new RecurringEvent
        {
            Name = "Orig",
            Weekday = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Frequency = RecurringFrequency.Weekly,
            StartDate = DateTime.Today,
            AdminId = adminId,
        };
        db.RecurringEvents.Add(ev);
        await db.SaveChangesAsync();
        return ev.Id;
    }

    private static void SetCaller(Role role, int id)
    {
        TestAuthHandler.Claims = new List<Claim> { new("id", id.ToString()), new(ClaimTypes.Role, role.ToString()) };
    }

    private static void ResetAuthDefault()
    {
        TestAuthHandler.Claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Role, "Admin"),
        };
    }

    private async Task<HttpResponseMessage> AsAsync(Role role, int id, Func<HttpClient, Task<HttpResponseMessage>> call)
    {
        SetCaller(role, id);
        try { return await call(CreateClient()); }
        finally { ResetAuthDefault(); }
    }

    // --- Availability ownership ---

    [Fact]
    public async Task Availability_Create_ForeignAdminId_Forbidden()
    {
        await ResetDbAsync();
        var teacherId = await AddUserAsync(Role.Teacher, "t.avail@test.se");
        var otherId = await AddUserAsync(Role.Admin, "a.avail@test.se");

        var resp = await AsAsync(Role.Teacher, teacherId, c => c.PostAsJsonAsync("/api/availability",
            new AddAvailabilityDto { AdminId = otherId, StartTime = DateTime.Today.AddHours(9), EndTime = DateTime.Today.AddHours(10) }));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.AdminAvailabilities.Any().Should().BeFalse("a foreign-owner overlay must not be persisted");
    }

    [Fact]
    public async Task Availability_Create_OwnAdminId_Allowed()
    {
        await ResetDbAsync();
        var teacherId = await AddUserAsync(Role.Teacher, "t.avail2@test.se");

        var resp = await AsAsync(Role.Teacher, teacherId, c => c.PostAsJsonAsync("/api/availability",
            new AddAvailabilityDto { AdminId = teacherId, StartTime = DateTime.Today.AddHours(9), EndTime = DateTime.Today.AddHours(10) }));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // --- Recurring-event ownership ---

    [Fact]
    public async Task RecurringEvent_Update_NonOwnerTeacher_Forbidden()
    {
        await ResetDbAsync();
        var owner = await AddUserAsync(Role.Teacher, "owner.re@test.se");
        var other = await AddUserAsync(Role.Teacher, "other.re@test.se");
        var evId = await AddRecurringEventAsync(owner);

        var resp = await AsAsync(Role.Teacher, other, c =>
            c.PutAsJsonAsync($"/api/recurring-events/{evId}", new UpdateRecurringEventDto { Name = "Hacked" }));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.RecurringEvents.FindAsync(evId))!.Name.Should().Be("Orig", "a non-owner edit must be rejected");
    }

    [Fact]
    public async Task RecurringEvent_Update_Owner_Allowed()
    {
        await ResetDbAsync();
        var owner = await AddUserAsync(Role.Teacher, "owner2.re@test.se");
        var evId = await AddRecurringEventAsync(owner);

        var resp = await AsAsync(Role.Teacher, owner, c =>
            c.PutAsJsonAsync($"/api/recurring-events/{evId}", new UpdateRecurringEventDto { Name = "Updated" }));

        resp.StatusCode.Should().Be(HttpStatusCode.OK, "the owner may edit their own event");
    }

    [Fact]
    public async Task RecurringEvent_Update_AdminNonOwner_Allowed()
    {
        await ResetDbAsync();
        var owner = await AddUserAsync(Role.Teacher, "owner3.re@test.se");
        var admin = await AddUserAsync(Role.Admin, "admin.re@test.se");
        var evId = await AddRecurringEventAsync(owner);

        var resp = await AsAsync(Role.Admin, admin, c =>
            c.PutAsJsonAsync($"/api/recurring-events/{evId}", new UpdateRecurringEventDto { Name = "AdminEdit" }));

        resp.StatusCode.Should().Be(HttpStatusCode.OK, "an admin may edit any event");
    }

    // --- Absence-warning recipient validation ---

    [Fact]
    public async Task AbsenceWarning_NonCoachRecipient_Rejected()
    {
        await ResetDbAsync();
        var adminId = await AddUserAsync(Role.Admin, "admin.aw@test.se");
        var fake = (FakeEmailService)Factory.Services.GetRequiredService<IEmailService>();
        fake.Clear();

        var resp = await AsAsync(Role.Admin, adminId, c => c.PostAsJsonAsync("/api/absence-warning/send",
            new SendAbsenceWarningDto("attacker@external.test", "Sub", "Body")));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        fake.Sent.Should().BeEmpty("nothing should be sent to a non-coach address");
    }

    [Fact]
    public async Task AbsenceWarning_CoachRecipient_Sent()
    {
        await ResetDbAsync();
        var adminId = await AddUserAsync(Role.Admin, "admin.aw2@test.se");
        await AddUserAsync(Role.Coach, "real.coach@test.se");
        var fake = (FakeEmailService)Factory.Services.GetRequiredService<IEmailService>();
        fake.Clear();

        var resp = await AsAsync(Role.Admin, adminId, c => c.PostAsJsonAsync("/api/absence-warning/send",
            new SendAbsenceWarningDto("real.coach@test.se", "Sub", "Body")));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        fake.Sent.Should().ContainSingle(e => e.To == "real.coach@test.se");
    }

    // --- GET /api/bookings staff-only ---

    [Fact]
    public async Task Bookings_Get_GuestRole_Forbidden()
    {
        await ResetDbAsync();

        var resp = await AsAsync(Role.Guest, 0, c => c.GetAsync("/api/bookings"));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden, "a Guest token must not receive the booking list");
    }

    [Fact]
    public async Task Bookings_Get_Admin_Ok()
    {
        await ResetDbAsync();
        var adminId = await AddUserAsync(Role.Admin, "admin.bk@test.se");

        var resp = await AsAsync(Role.Admin, adminId, c => c.GetAsync("/api/bookings"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
