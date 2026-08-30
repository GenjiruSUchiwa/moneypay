using MoniPay.Kernel;
using Xunit;

namespace MoniPay.Tests.Kernel;

public sealed class UserIdTests
{
    [Fact]
    public void New_creates_a_nonempty_version_seven_identifier()
    {
        UserId userId = UserId.New();

        Assert.NotEqual(Guid.Empty, userId.Value);
        Assert.Equal(7, userId.Value.Version);
    }

    [Fact]
    public void A_user_id_round_trips_through_its_d_format()
    {
        UserId expected = UserId.New();
        string text = expected.ToString();

        UserId parsed = UserId.Parse(text);

        Assert.Equal(expected, parsed);
        Assert.Equal(expected.Value.ToString("D"), text);
    }

    [Fact]
    public void TryParse_returns_a_user_id_for_a_valid_guid()
    {
        string text = "018f0f4d-7b9e-7d12-8c2a-0f6b8a7c4d11";

        bool parsed = UserId.TryParse(text, out UserId userId);

        Assert.True(parsed);
        Assert.Equal(text, userId.ToString());
    }

    [Fact]
    public void Invalid_user_id_text_cannot_be_parsed()
    {
        const string invalidText = "not-a-user-id";

        bool parsed = UserId.TryParse(invalidText, out UserId userId);

        Assert.False(parsed);
        Assert.Equal(default(UserId), userId);
        Assert.Throws<FormatException>(() => UserId.Parse(invalidText));
    }
}
