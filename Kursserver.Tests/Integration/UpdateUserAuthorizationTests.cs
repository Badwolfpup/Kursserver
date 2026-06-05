using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Kursserver.Tests.Integration;

/// <summary>
/// Regression guard for the privilege-escalation fix on PUT /api/update-user.
/// Before the fix the endpoint used IsTeacher(context, 1, 1), which let any Coach (and any
/// Teacher) set an arbitrary AuthLevel on any user — i.e. self-promote to Admin — and edit
/// privileged (Admin/Teacher) accounts (e.g. swap an admin's email to take it over).
/// The endpoint now: (a) admits only staff, (b) requires Admin to touch an Admin/Teacher
/// account unless editing yourself, and (c) requires Admin to change any user's role.
/// Legitimate flows preserved: staff (incl. Coach) editing a participant's non-role fields,
/// and any staff member editing their own profile.
/// </summary>
public class UpdateUserAuthorizationTests : IntegrationTestBase
{
    public UpdateUserAuthorizationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private sealed record Seeded(int AdminId, int TeacherId, int CoachId, int StudentId);

    private async Task<Seeded> SeedUsersAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();

        var admin = new User { FirstName = "Anna", LastName = "Admin", Email = "anna.admin@test.se", AuthLevel = Role.Admin };
        var teacher = new User { FirstName = "Tina", LastName = "Teacher", Email = "tina.teacher@test.se", AuthLevel = Role.Teacher };
        var coach = new User { FirstName = "Cara", LastName = "Coach", Email = "cara.coach@test.se", AuthLevel = Role.Coach };
        var student = new User { FirstName = "Sam", LastName = "Student", Email = "sam.student@test.se", AuthLevel = Role.Student };
        db.Users.AddRange(admin, teacher, coach, student);
        await db.SaveChangesAsync();
        return new Seeded(admin.Id, teacher.Id, coach.Id, student.Id);
    }

    private static void SetCaller(Role role, int id)
    {
        TestAuthHandler.Claims = new List<Claim>
        {
            new("id", id.ToString()),
            new(ClaimTypes.Role, role.ToString()),
        };
    }

    // Restore the shared static identity to the default Admin so other test classes are unaffected.
    private static void ResetAuthDefault()
    {
        TestAuthHandler.Claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Role, "Admin"),
        };
    }

    private async Task<HttpResponseMessage> UpdateAsAsync(Role callerRole, int callerId, UpdateUserDto dto)
    {
        SetCaller(callerRole, callerId);
        try { return await CreateClient().PutAsJsonAsync("/api/update-user", dto); }
        finally { ResetAuthDefault(); }
    }

    private async Task<User> GetUserAsync(int id)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (await db.Users.FindAsync(id))!;
    }

    [Fact]
    public async Task Coach_CannotPromoteStudentToAdmin()
    {
        var s = await SeedUsersAsync();
        var resp = await UpdateAsAsync(Role.Coach, s.CoachId,
            new UpdateUserDto { Id = s.StudentId, AuthLevel = Role.Admin });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await GetUserAsync(s.StudentId)).AuthLevel.Should().Be(Role.Student, "the role change must be rejected");
    }

    [Fact]
    public async Task Coach_CannotPromoteSelfToAdmin()
    {
        var s = await SeedUsersAsync();
        var resp = await UpdateAsAsync(Role.Coach, s.CoachId,
            new UpdateUserDto { Id = s.CoachId, AuthLevel = Role.Admin });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await GetUserAsync(s.CoachId)).AuthLevel.Should().Be(Role.Coach, "a coach must not self-promote");
    }

    [Fact]
    public async Task Coach_CannotEditAdminAccount()
    {
        var s = await SeedUsersAsync();
        var resp = await UpdateAsAsync(Role.Coach, s.CoachId,
            new UpdateUserDto { Id = s.AdminId, Email = "attacker@evil.test" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await GetUserAsync(s.AdminId)).Email.Should().Be("anna.admin@test.se", "an admin's email must not be hijackable by a coach");
    }

    [Fact]
    public async Task Coach_CanEditStudentNonRoleFields()
    {
        var s = await SeedUsersAsync();
        // Mirrors the CoachAttendance flow: the full user object (incl. unchanged AuthLevel) is sent.
        var resp = await UpdateAsAsync(Role.Coach, s.CoachId,
            new UpdateUserDto { Id = s.StudentId, AuthLevel = Role.Student, Telephone = "070-111 22 33" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK, "staff may edit a participant's non-role fields");
        (await GetUserAsync(s.StudentId)).Telephone.Should().Be("070-111 22 33");
    }

    [Fact]
    public async Task Teacher_CannotPromoteStudentToAdmin()
    {
        var s = await SeedUsersAsync();
        var resp = await UpdateAsAsync(Role.Teacher, s.TeacherId,
            new UpdateUserDto { Id = s.StudentId, AuthLevel = Role.Admin });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await GetUserAsync(s.StudentId)).AuthLevel.Should().Be(Role.Student);
    }

    [Fact]
    public async Task Teacher_CannotEditAnotherTeacher()
    {
        var s = await SeedUsersAsync();
        // Seed a second teacher to act as the victim.
        int victimId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var victim = new User { FirstName = "Bo", LastName = "Teacher2", Email = "bo.teacher@test.se", AuthLevel = Role.Teacher };
            db.Users.Add(victim);
            await db.SaveChangesAsync();
            victimId = victim.Id;
        }
        var resp = await UpdateAsAsync(Role.Teacher, s.TeacherId,
            new UpdateUserDto { Id = victimId, Email = "attacker@evil.test" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await GetUserAsync(victimId)).Email.Should().Be("bo.teacher@test.se");
    }

    [Fact]
    public async Task Teacher_CanEditOwnProfile()
    {
        var s = await SeedUsersAsync();
        var resp = await UpdateAsAsync(Role.Teacher, s.TeacherId,
            new UpdateUserDto { Id = s.TeacherId, Telephone = "070-999 88 77" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK, "a teacher may edit their own profile");
        (await GetUserAsync(s.TeacherId)).Telephone.Should().Be("070-999 88 77");
    }

    [Fact]
    public async Task Admin_CanChangeUserRole()
    {
        var s = await SeedUsersAsync();
        var resp = await UpdateAsAsync(Role.Admin, s.AdminId,
            new UpdateUserDto { Id = s.StudentId, AuthLevel = Role.Teacher });
        resp.StatusCode.Should().Be(HttpStatusCode.OK, "an admin may change roles");
        (await GetUserAsync(s.StudentId)).AuthLevel.Should().Be(Role.Teacher);
    }
}
