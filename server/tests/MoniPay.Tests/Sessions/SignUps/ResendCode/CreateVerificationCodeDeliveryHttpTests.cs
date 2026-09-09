using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.ResendCode;

/// <summary>
/// The resend route: a command with no body, authorized by the sign-up credential, that replaces
/// the code without resetting the attempt budget or extending the workflow.
/// </summary>
public sealed class CreateVerificationCodeDeliveryHttpTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SignUpLifetime = TimeSpan.FromMinutes(15);
    private const int MaximumAttempts = 3;
    private const int MaximumResends = 3;

    [Fact]
    public async Task A_resend_returns_202_the_state_resource_and_its_location()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        Api.Time.Advance(ResendCooldown);

        using HttpResponseMessage response = await SignUpFlow.PostResendAsync(
            Client,
            started.SignUpId,
            started.SignUpToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());

        JsonElement document = await JsonApiAssertions.ReadJsonApiAsync(
            response,
            SignUpResourceTypes.SignUps,
            started.SignUpId.Value.ToString());
        JsonElement attributes = document.GetProperty("data").GetProperty("attributes");
        Assert.Equal(JsonApiAssertions.Wire(SignUpStatusValue.CodePending), attributes.GetProperty("status").GetString());
        Assert.Equal(JsonApiAssertions.Wire(CodeDeliveryValue.Queued), attributes.GetProperty("codeDelivery").GetString());
        Assert.False(attributes.TryGetProperty("signUpToken", out _));
        Assert.False(attributes.TryGetProperty("registrationToken", out _));

        string self = SignUpResources.Self(started.SignUpId);
        Assert.Equal(self, document.GetProperty("data").GetProperty("links").GetProperty("self").GetString());
        Assert.Equal(self, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task A_resend_replaces_the_code_and_keeps_the_attempt_budget()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string priorCode = await Api.DeliveredCodeAsync(phone);
        await SignUpFlow.RefusedAsync(
            Api.VerifyPhoneAsync(started.SignUpId, SignUpFlow.Wrong(priorCode)),
            MoniPayErrorTypes.VerificationCodeInvalid);
        Api.Time.Advance(ResendCooldown);

        using HttpResponseMessage response = await SignUpFlow.PostResendAsync(
            Client,
            started.SignUpId,
            started.SignUpToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        JsonElement attributes = (await JsonApiAssertions.ReadJsonApiAsync(response))
            .GetProperty("data")
            .GetProperty("attributes");
        Assert.Equal(started.SignUpExpiresAt, attributes.GetProperty("signUpExpiresAt").GetDateTimeOffset());
        Assert.Equal(Api.Time.GetUtcNow() + CodeLifetime, attributes.GetProperty("codeExpiresAt").GetDateTimeOffset());

        // A resend never resets the budget: the one failed attempt is still counted.
        Assert.Equal(1, (await Api.ReadSignUpRowAsync(started.SignUpId)).FailedAttempts);

        IReadOnlyList<MoniPay.Notifications.Domain.Notification> deliveries =
            await SignUpFlow.NotificationsForAsync(Api, started.SignUpId);
        Assert.Equal(2, deliveries.Count);
        Assert.Equal($"verification-code:{started.SignUpId}:1", deliveries[^1].IdempotencyKey);
        Assert.Equal(
            attributes.GetProperty("codeExpiresAt").GetDateTimeOffset(),
            deliveries[^1].ExpiresAt);

        string newCode = await Api.DeliveredCodeAsync(phone);
        Assert.NotEqual(priorCode, newCode);
        await SignUpFlow.RefusedAsync(
            Api.VerifyPhoneAsync(started.SignUpId, priorCode),
            MoniPayErrorTypes.VerificationCodeInvalid);
        Assert.NotNull(await Api.VerifyPhoneAsync(started.SignUpId, newCode));
    }

    [Fact]
    public async Task A_resend_is_possible_once_the_code_expired()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        Api.Time.Advance(CodeLifetime + TimeSpan.FromSeconds(1));

        using HttpResponseMessage response = await SignUpFlow.PostResendAsync(
            Client,
            started.SignUpId,
            started.SignUpToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        JsonElement attributes = (await JsonApiAssertions.ReadJsonApiAsync(response))
            .GetProperty("data")
            .GetProperty("attributes");
        Assert.Equal(Api.Time.GetUtcNow() + CodeLifetime, attributes.GetProperty("codeExpiresAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task A_resend_inside_the_cooldown_is_429_with_a_retry_delay()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        Api.Time.Advance(ResendCooldown - TimeSpan.FromSeconds(1));

        using HttpResponseMessage response = await SignUpFlow.PostResendAsync(
            Client,
            started.SignUpId,
            started.SignUpToken);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.TooManyRequests);
        Assert.Equal(MoniPayErrorTypes.SignUpResendTooSoon.Urn, problem.Type);
        Assert.NotNull(response.Headers.RetryAfter);

        // The boundary itself is a permitted resend: the cooldown elapsed, it did not nearly elapse.
        Api.Time.Advance(TimeSpan.FromSeconds(1));
        using HttpResponseMessage atBoundary = await SignUpFlow.PostResendAsync(
            Client,
            started.SignUpId,
            started.SignUpToken);
        Assert.Equal(HttpStatusCode.Accepted, atBoundary.StatusCode);
    }

    [Fact]
    public async Task A_resend_beyond_the_limit_is_429_with_a_retry_delay()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));

        for (int resend = 0; resend < MaximumResends; resend++)
        {
            Api.Time.Advance(ResendCooldown);
            using HttpResponseMessage accepted = await SignUpFlow.PostResendAsync(
                Client,
                started.SignUpId,
                started.SignUpToken);
            Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        }

        Api.Time.Advance(ResendCooldown);
        using HttpResponseMessage refused = await SignUpFlow.PostResendAsync(
            Client,
            started.SignUpId,
            started.SignUpToken);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await refused.ReadProblemAsync(HttpStatusCode.TooManyRequests);
        Assert.Equal(MoniPayErrorTypes.SignUpResendLimit.Urn, problem.Type);
        Assert.NotNull(refused.Headers.RetryAfter);
    }

    [Fact]
    public async Task A_locked_sign_up_refuses_a_resend_as_a_state_conflict()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string wrong = SignUpFlow.Wrong(await Api.DeliveredCodeAsync(phone));
        for (int attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            await SignUpFlow.RefusalOfAsync(Api.VerifyPhoneAsync(started.SignUpId, wrong));
        }

        Api.Time.Advance(ResendCooldown);

        using HttpResponseMessage response = await SignUpFlow.PostResendAsync(
            Client,
            started.SignUpId,
            started.SignUpToken);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(MoniPayErrorTypes.SignUpStateInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task An_expired_workflow_is_refused_at_authentication()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        Api.Time.Advance(SignUpLifetime);

        using HttpResponseMessage response = await SignUpFlow.PostResendAsync(
            Client,
            started.SignUpId,
            started.SignUpToken);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SignUpTokenInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task A_missing_credential_is_401_with_the_sign_up_challenge()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        using HttpRequestMessage request = new(HttpMethod.Post, SignUpFlow.ResendUrl(started.SignUpId));

        using HttpResponseMessage response = await Client.SendAsync(request, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SignUpTokenInvalid.Urn, problem.Type);
        Assert.Equal(SessionsSchemes.SignUp, response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task A_credential_bound_to_another_sign_up_does_not_open_the_route()
    {
        StartSignUpResult other = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        StartSignUpResult mine = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));

        using HttpResponseMessage response = await SignUpFlow.PostResendAsync(
            Client,
            mine.SignUpId,
            other.SignUpToken);

        await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_jsonapi_body_is_415_before_it_is_read()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        using StringContent body = new(
            """{"data":{"type":"verification-code-deliveries","attributes":{}}}""",
            Encoding.UTF8);
        body.Headers.ContentType = MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);

        using HttpResponseMessage response = await SignUpFlow.PostResendAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            body);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnsupportedMediaType);
        Assert.Equal(MoniPayErrorTypes.UnsupportedMediaType.Urn, problem.Type);
    }

    [Fact]
    public async Task A_chunked_body_is_415_before_it_is_read()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        using StringContent body = new("""{"data":{"type":"verification-code-deliveries"}}""", Encoding.UTF8);
        body.Headers.ContentType = MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);
        body.Headers.ContentLength = null;

        using HttpResponseMessage response = await SignUpFlow.PostResendAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            body);

        await response.ReadProblemAsync(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Accepting_only_problem_json_is_406()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.ProblemJson);

        using HttpResponseMessage response = await SignUpFlow.PostResendAsync(
            client,
            started.SignUpId,
            started.SignUpToken);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.NotAcceptable);
        Assert.Equal(MoniPayErrorTypes.NotAcceptable.Urn, problem.Type);
    }

    [Theory]
    [InlineData("en", "The verification code was requested again too soon.")]
    [InlineData("fr", "Le code de vérification a été redemandé trop tôt.")]
    public async Task Problems_are_localized(string culture, string title)
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);

        using HttpResponseMessage response = await SignUpFlow.PostResendAsync(
            client,
            started.SignUpId,
            started.SignUpToken);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.TooManyRequests);
        Assert.Equal(MoniPayErrorTypes.SignUpResendTooSoon.Urn, problem.Type);
        Assert.Equal(title, problem.Title);
    }
}
