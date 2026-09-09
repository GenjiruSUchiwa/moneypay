using MoniPay.Notifications.Security;
using Xunit;

namespace MoniPay.Tests.Notifications.Security;

public sealed class RecipientProtectorTests
{
    [Theory]
    [InlineData("+237670123456", "3456")]
    [InlineData("+1 202 555 0123", "0123")]
    [InlineData("account@proton.me", "proton.m")]
    [InlineData("short@ac.me", "ac.me")]
    [InlineData("+123", "123")]
    public void The_hint_carries_only_the_documented_sliver(string recipient, string expected) =>
        Assert.Equal(expected, RecipientProtector.Hint(recipient));
}
