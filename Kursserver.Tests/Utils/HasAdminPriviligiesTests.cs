using FluentAssertions;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Kursserver.Tests.Utils;

public class HasAdminPriviligiesTests
{
    private static HttpContext MakeContext(Role role)
    {
        var context = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.Role, role.ToString()) };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        return context;
    }

    // --- IsTeacher(context, affectedUserRole) ---

    [Fact]
    public void IsTeacher_AdminCallerAffectingStudent_ReturnsNull()
    {
        var ctx = MakeContext(Role.Admin);
        HasAdminPriviligies.IsTeacher(ctx, (int)Role.Student).Should().BeNull();
    }

    [Fact]
    public void IsTeacher_TeacherCallerAffectingStudent_ReturnsNull()
    {
        var ctx = MakeContext(Role.Teacher);
        HasAdminPriviligies.IsTeacher(ctx, (int)Role.Student).Should().BeNull();
    }

    [Fact]
    public void IsTeacher_StudentCaller_Returns403()
    {
        var ctx = MakeContext(Role.Student);
        var result = HasAdminPriviligies.IsTeacher(ctx, (int)Role.Student);
        result.Should().NotBeNull();
        var httpResult = result as Microsoft.AspNetCore.Http.IResult;
        httpResult.Should().NotBeNull();
        // StatusCode 403
        var statusResult = result as Microsoft.AspNetCore.Http.HttpResults.StatusCodeHttpResult;
        statusResult!.StatusCode.Should().Be(403);
    }

    [Fact]
    public void IsTeacher_TeacherCallerAffectingTeacher_Returns401()
    {
        var ctx = MakeContext(Role.Teacher);
        var result = HasAdminPriviligies.IsTeacher(ctx, (int)Role.Teacher);
        result.Should().NotBeNull();
        var statusResult = result as Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult;
        statusResult.Should().NotBeNull();
    }

    [Fact]
    public void IsTeacher_AdminCallerAffectingTeacher_ReturnsNull()
    {
        var ctx = MakeContext(Role.Admin);
        HasAdminPriviligies.IsTeacher(ctx, (int)Role.Teacher).Should().BeNull();
    }

    // --- IsTeacher(context, affectedUserRole, coach) overload ---

    [Fact]
    public void IsTeacherWithCoach_CoachCaller_ReturnsNull()
    {
        var ctx = MakeContext(Role.Coach);
        HasAdminPriviligies.IsTeacher(ctx, (int)Role.Student, 1).Should().BeNull();
    }

    [Fact]
    public void IsTeacherWithCoach_StudentCaller_Returns403()
    {
        var ctx = MakeContext(Role.Student);
        var result = HasAdminPriviligies.IsTeacher(ctx, (int)Role.Student, 1);
        result.Should().NotBeNull();
        var statusResult = result as Microsoft.AspNetCore.Http.HttpResults.StatusCodeHttpResult;
        statusResult!.StatusCode.Should().Be(403);
    }
}
