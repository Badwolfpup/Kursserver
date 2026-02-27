using FluentAssertions;
using Kursserver.Utils;

namespace Kursserver.Tests.Helpers;

public class AuthHelpersTests
{
    [Fact]
    public void IsLockedOut_14Minutes_ReturnsTrue()
    {
        var lockoutStart = DateTime.UtcNow.AddMinutes(-14);
        AuthHelpers.IsLockedOut(lockoutStart, DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsLockedOut_15Minutes_ReturnsFalse()
    {
        var lockoutStart = DateTime.UtcNow.AddMinutes(-15);
        AuthHelpers.IsLockedOut(lockoutStart, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_JustUnder15Minutes_ReturnsTrue()
    {
        var lockoutStart = DateTime.UtcNow.AddMinutes(-14).AddSeconds(-59);
        AuthHelpers.IsLockedOut(lockoutStart, DateTime.UtcNow).Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 30)]
    [InlineData(3, 6)]
    [InlineData(4, 6)]
    [InlineData(5, 6)]
    public void GetTokenExpiryDays_ReturnsCorrectDays(int authLevel, int expectedDays)
    {
        AuthHelpers.GetTokenExpiryDays(authLevel).Should().Be(expectedDays);
    }
}
