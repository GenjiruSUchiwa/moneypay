using System.Net;
using System.Net.Http.Json;
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
            (HttpStatusCode inflightStartStatus, JsonElement inflightStart) = await StartAsync(inflight);
            Assert.Equal(HttpStatusCode.Accepted, inflightStartStatus);
            string inflightId = IdOf(inflightStart);

            Api.Time.Advance(TimeSpan.FromSeconds(61));

            (HttpStatusCode firstStatus, JsonElement first) = await StartAsync(fresh);
            (HttpStatusCode secondStatus, JsonElement second) = await StartAsync(inflight);
            (HttpStatusCode thirdStatus, JsonElement third) = await StartAsync(registered);

            Assert.Equal(HttpStatusCode.Accepted, firstStatus);
            Assert.Equal(HttpStatusCode.Accepted, secondStatus);
            Assert.Equal(HttpStatusCode.Accepted, thirdStatus);

            Assert.Equal(SignUpStatuses.CodePending, StatusOf(first));
            Assert.Equal(SignUpStatuses.CodePending, StatusOf(second));
            Assert.Equal(SignUpStatuses.CodePending, StatusOf(third));

            Assert.Equal(inflightId, IdOf(second));
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

    private async Task<(HttpStatusCode Status, JsonElement Document)> StartAsync(PhoneNumber phone)
    {
        using StringContent body = SignUpFlow.StartBody(phone.Value);
        using HttpResponseMessage response = await SignUpFlow.PostStartAsync(Client, body);

        JsonElement document = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);

        return (response.StatusCode, document);
    }

    private static string StatusOf(JsonElement document) =>
        document.GetProperty("data").GetProperty("attributes").GetProperty("status").GetString()
        ?? throw new InvalidOperationException("The sign-up status was not a JSON string.");

    private static string IdOf(JsonElement document) =>
        document.GetProperty("data").GetProperty("id").GetString()
        ?? throw new InvalidOperationException("The sign-up identifier was not a JSON string.");

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
