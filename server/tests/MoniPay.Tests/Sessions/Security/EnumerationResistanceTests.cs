using System.Net;
using System.Text.Json;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.Security;

public sealed class EnumerationResistanceTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task New_in_flight_and_registered_phones_share_the_public_shape()
    {
        PhoneNumber fresh = new(TestPhones.Next());
        PhoneNumber inflight = new(TestPhones.Next());
        PhoneNumber registered = new(TestPhones.Next());
        await Api.RegisterUserAsync(registered);

        try
        {
            string inflightId = JsonApiAssertions.IdOf(await StartAsync(inflight));

            // Inside the resend cooldown an in-flight phone answers 429 where the others answer
            // 202. That transient oracle is accepted and documented; the test pins it so a change
            // to the trade-off is deliberate.
            using (StringContent tooSoon = SignUpFlow.StartBody(inflight.Value))
            using (HttpResponseMessage cooldown = await SignUpFlow.PostStartAsync(Client, tooSoon))
            {
                Microsoft.AspNetCore.Mvc.ProblemDetails problem =
                    await cooldown.ReadProblemAsync(HttpStatusCode.TooManyRequests);
                Assert.Equal(MoniPayErrorTypes.SignUpResendTooSoon.Urn, problem.Type);
            }

            Api.Time.Advance(TimeSpan.FromSeconds(61));

            JsonElement first = await StartAsync(fresh);
            JsonElement second = await StartAsync(inflight);
            JsonElement third = await StartAsync(registered);

            string codePending = JsonApiAssertions.Wire(SignUpStatusValue.CodePending);
            Assert.Equal(codePending, JsonApiAssertions.StatusOf(first));
            Assert.Equal(codePending, JsonApiAssertions.StatusOf(second));
            Assert.Equal(codePending, JsonApiAssertions.StatusOf(third));

            Assert.Equal(inflightId, JsonApiAssertions.IdOf(second));
            Assert.Equal(PublicShape(first), PublicShape(second));
            Assert.Equal(PublicShape(first), PublicShape(third));
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_wrong_credential_answers_the_same_401_whatever_exists()
    {
        MoniPay.Sessions.Features.SignUps.Start.StartSignUpResult started =
            await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));

        using HttpResponseMessage known = await SignUpFlow.GetSignUpHttpAsync(
            Client,
            started.SignUpId,
            SessionsSchemes.SignUp,
            SignUpFlow.Wrong(started.SignUpToken));
        using HttpResponseMessage unknown = await SignUpFlow.GetSignUpHttpAsync(
            Client,
            SignUpId.New(),
            SessionsSchemes.SignUp,
            started.SignUpToken);

        foreach (HttpResponseMessage response in new[] { known, unknown })
        {
            Microsoft.AspNetCore.Mvc.ProblemDetails problem =
                await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
            Assert.Equal(MoniPayErrorTypes.SignUpTokenInvalid.Urn, problem.Type);
        }
    }

    private async Task<JsonElement> StartAsync(PhoneNumber phone)
    {
        using StringContent body = SignUpFlow.StartBody(phone.Value);
        using HttpResponseMessage response = await SignUpFlow.PostStartAsync(Client, body);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        return await response.ReadJsonApiAsync();
    }

    private static IReadOnlyList<(string Name, JsonValueKind Kind)> PublicShape(JsonElement document)
    {
        // Tokens and timestamps vary by request; their JSON kinds still belong in the comparison.
        return document
            .GetProperty("data")
            .GetProperty("attributes")
            .EnumerateObject()
            .Select(property => (property.Name, property.Value.ValueKind))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();
    }
}
