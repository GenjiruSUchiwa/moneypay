using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel;
using MoniPay.Sessions;
using MoniPay.Sessions.Providers;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Delivery;

public sealed class VerificationCodeRenderingTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task Stored_locales_render_their_own_text()
    {
        VerificationCodeRenderer renderer = Api.Services.GetRequiredService<VerificationCodeRenderer>();

        string french = renderer.Render(Locale.French, "001234", TimeSpan.FromMinutes(2));
        string cameroon = renderer.Render(Locale.FrenchCameroon, "001234", TimeSpan.FromMinutes(2));
        string english = renderer.Render(Locale.English, "001234", TimeSpan.FromMinutes(2));

        Assert.Contains("001234", french, StringComparison.Ordinal);
        Assert.Contains("votre code", cameroon, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("your verification code", english, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("001234", cameroon, StringComparison.Ordinal);
        Assert.Contains("001234", english, StringComparison.Ordinal);
    }

    [Fact]
    public Task Rendering_restores_the_ambient_culture()
    {
        VerificationCodeRenderer renderer = Api.Services.GetRequiredService<VerificationCodeRenderer>();
        CultureInfo beforeCulture = CultureInfo.CurrentCulture;
        CultureInfo beforeUi = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            renderer.Render(Locale.French, "001234", TimeSpan.FromMinutes(2));
            Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
            Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);

            Assert.ThrowsAny<Exception>(() => renderer.Render(Locale.French, "", TimeSpan.FromMinutes(2)));
            Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
            Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
        }
        finally
        {
            CultureInfo.CurrentCulture = beforeCulture;
            CultureInfo.CurrentUICulture = beforeUi;
        }

        return Task.CompletedTask;
    }

    [Fact]
    public void Partial_minutes_are_never_truncated()
    {
        VerificationCodeRenderer renderer = Api.Services.GetRequiredService<VerificationCodeRenderer>();

        string body = renderer.Render(Locale.English, "001234", TimeSpan.FromSeconds(90));

        Assert.Contains("It expires in 2 minutes.", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delivered_bodies_carry_only_the_code_the_lifetime_and_the_product()
    {
        PhoneNumber phone = new(TestPhones.Next());
        await Api.StartSignUpAsync(phone);
        string code = await Api.DeliveredCodeAsync(phone);
        string body = Api.Sms.CallsFor(phone.Value)[0].Body;

        Assert.Contains("MoniPay", body, StringComparison.Ordinal);
        Assert.Contains(code, body, StringComparison.Ordinal);
        Assert.Contains("Il expire dans 2 minutes.", body, StringComparison.Ordinal);
        Assert.DoesNotContain(phone.Value, body, StringComparison.Ordinal);
        Assert.DoesNotContain("@", body, StringComparison.Ordinal);
        Assert.DoesNotContain("http", body, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await Api.RowsContainingAsync(body));
    }

    [Fact]
    public void The_message_hides_everything_it_carries()
    {
        VerificationCodeMessage message = new(
            SignUpId.New(),
            new PhoneNumber(TestPhones.Next()),
            "MoniPay : votre code de vérification est 001234. Il expire dans 2 minutes.",
            DateTimeOffset.UtcNow,
            "verification-code:00000000-0000-0000-0000-000000000000:0");

        Assert.Equal(nameof(VerificationCodeMessage), message.ToString());
        Assert.DoesNotContain("001234", message.ToString(), StringComparison.Ordinal);
    }
}
