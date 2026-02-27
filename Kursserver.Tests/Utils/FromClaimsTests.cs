using FluentAssertions;
using Kursserver.Utils;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Kursserver.Tests.Utils;

public class FromClaimsTests
{
    private static HttpContext MakeContext(string? idClaimValue)
    {
        var context = new DefaultHttpContext();
        if (idClaimValue != null)
        {
            var claims = new[] { new Claim("id", idClaimValue) };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
        }
        return context;
    }

    [Fact]
    public void GetUserId_ValidIntClaim_ReturnsInt()
    {
        var context = MakeContext("42");
        new FromClaims().GetUserId(context).Should().Be(42);
    }

    [Fact]
    public void GetUserId_MissingClaim_ReturnsZero()
    {
        var context = MakeContext(null);
        new FromClaims().GetUserId(context).Should().Be(0);
    }

    [Fact]
    public void GetUserId_NonNumericClaim_ReturnsZero()
    {
        var context = MakeContext("not-a-number");
        new FromClaims().GetUserId(context).Should().Be(0);
    }
}
