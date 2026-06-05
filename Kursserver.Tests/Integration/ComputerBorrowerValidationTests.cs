using System.Net;
using System.Net.Http.Json;
using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Kursserver.Tests.Integration;

/// <summary>
/// Covers the borrower-role validation on the computer endpoints: students, teachers and
/// admins may be assigned a computer (as owner or per day/period slot); coaches and guests
/// may not. Regression guard for the change that opened borrowing up to staff, not just
/// students — and that nothing stops staff borrowing more than one machine.
/// </summary>
public class ComputerBorrowerValidationTests : IntegrationTestBase
{
    public ComputerBorrowerValidationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // The factory (IClassFixture) is shared across this class's tests, so the in-memory DB
    // persists between them — reset it, then seed one computer and one user of the given role.
    private async Task<(int ComputerId, int UserId)> SeedComputerAndUserAsync(Role role)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.ComputerAssignments.RemoveRange(db.ComputerAssignments);
        db.Computers.RemoveRange(db.Computers);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();

        var user = new User
        {
            FirstName = "Test",
            LastName = role.ToString(),
            Email = $"{role}.borrower@test.se",
            AuthLevel = role,
        };
        db.Users.Add(user);
        var computer = new Computer { Number = 1 };
        db.Computers.Add(computer);
        await db.SaveChangesAsync();

        return (computer.Id, user.Id);
    }

    [Theory]
    [InlineData(Role.Student, true)]
    [InlineData(Role.Teacher, true)]
    [InlineData(Role.Admin, true)]
    [InlineData(Role.Coach, false)]
    [InlineData(Role.Guest, false)]
    public async Task SetOwner_AllowsOnlyBorrowerRoles(Role role, bool shouldBeAllowed)
    {
        var (computerId, userId) = await SeedComputerAndUserAsync(role);

        var client = CreateClient();
        var response = await client.PutAsJsonAsync(
            "/api/computers/owner",
            new SetComputerOwnerDto { ComputerId = computerId, StudentId = userId, TakesHome = false });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var owner = (await db.Computers.FindAsync(computerId))!.OwnerStudentId;

        if (shouldBeAllowed)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"{role} should be allowed to own a computer");
            owner.Should().Be(userId, "the owner should be persisted");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"{role} must not be allowed to own a computer");
            owner.Should().BeNull("a rejected borrower must not be persisted");
        }
    }

    [Theory]
    [InlineData(Role.Student, true)]
    [InlineData(Role.Teacher, true)]
    [InlineData(Role.Admin, true)]
    [InlineData(Role.Coach, false)]
    [InlineData(Role.Guest, false)]
    public async Task AssignSlot_AllowsOnlyBorrowerRoles(Role role, bool shouldBeAllowed)
    {
        var (computerId, userId) = await SeedComputerAndUserAsync(role);

        var client = CreateClient();
        var response = await client.PutAsJsonAsync(
            "/api/computer-assignments/assign",
            new AssignComputerDto { ComputerId = computerId, DayOfWeek = 1, Period = "am", StudentId = userId });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var assigned = db.ComputerAssignments.Any(a => a.ComputerId == computerId && a.StudentId == userId);

        if (shouldBeAllowed)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"{role} should be assignable to a slot");
            assigned.Should().BeTrue("the assignment should be persisted");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"{role} must not be assignable to a slot");
            assigned.Should().BeFalse("a rejected borrower must not be persisted");
        }
    }

    [Fact]
    public async Task SetOwner_AllowsStaffToOwnMultipleComputers()
    {
        // The "one computer per student" limit is enforced only in the frontend. Guard that
        // the backend has no borrower-unique constraint that would block staff from holding
        // several machines (the whole point of letting staff borrow).
        int teacherId, comp1Id, comp2Id;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.ComputerAssignments.RemoveRange(db.ComputerAssignments);
            db.Computers.RemoveRange(db.Computers);
            db.Users.RemoveRange(db.Users);
            await db.SaveChangesAsync();

            var teacher = new User { FirstName = "Tina", LastName = "Teacher", Email = "tina.teacher@test.se", AuthLevel = Role.Teacher };
            db.Users.Add(teacher);
            var c1 = new Computer { Number = 1 };
            var c2 = new Computer { Number = 2 };
            db.Computers.AddRange(c1, c2);
            await db.SaveChangesAsync();
            teacherId = teacher.Id; comp1Id = c1.Id; comp2Id = c2.Id;
        }

        var client = CreateClient();
        var r1 = await client.PutAsJsonAsync("/api/computers/owner", new SetComputerOwnerDto { ComputerId = comp1Id, StudentId = teacherId, TakesHome = false });
        var r2 = await client.PutAsJsonAsync("/api/computers/owner", new SetComputerOwnerDto { ComputerId = comp2Id, StudentId = teacherId, TakesHome = false });

        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        r2.StatusCode.Should().Be(HttpStatusCode.OK);

        using var assertScope = Factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        assertDb.Computers.Count(c => c.OwnerStudentId == teacherId)
            .Should().Be(2, "a teacher may own more than one computer");
    }
}
