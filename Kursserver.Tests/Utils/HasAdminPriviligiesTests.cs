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

    // --- CanManageUser(context, affectedRole) ---
    // Caller must be staff (Admin/Teacher); acting on a privileged target (Admin/Teacher) requires Admin.

    [Theory]
    [InlineData(Role.Student)]
    [InlineData(Role.Coach)]
    [InlineData(Role.Teacher)]
    [InlineData(Role.Admin)]
    public void CanManageUser_AdminCaller_AnyTarget_ReturnsNull(Role affected)
    {
        var ctx = MakeContext(Role.Admin);
        HasAdminPriviligies.CanManageUser(ctx, affected).Should().BeNull();
    }

    [Theory]
    [InlineData(Role.Student)]
    [InlineData(Role.Coach)]
    public void CanManageUser_TeacherCaller_NonPrivilegedTarget_ReturnsNull(Role affected)
    {
        var ctx = MakeContext(Role.Teacher);
        HasAdminPriviligies.CanManageUser(ctx, affected).Should().BeNull();
    }

    [Theory]
    [InlineData(Role.Teacher)]
    [InlineData(Role.Admin)]
    public void CanManageUser_TeacherCaller_PrivilegedTarget_Returns401(Role affected)
    {
        var ctx = MakeContext(Role.Teacher);
        var result = HasAdminPriviligies.CanManageUser(ctx, affected);
        (result as Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult).Should().NotBeNull();
    }

    [Theory]
    [InlineData(Role.Coach)]
    [InlineData(Role.Student)]
    [InlineData(Role.Guest)]
    public void CanManageUser_NonStaffCaller_Returns403(Role caller)
    {
        var ctx = MakeContext(caller);
        var result = HasAdminPriviligies.CanManageUser(ctx, Role.Student);
        var statusResult = result as Microsoft.AspNetCore.Http.HttpResults.StatusCodeHttpResult;
        statusResult!.StatusCode.Should().Be(403);
    }
}
