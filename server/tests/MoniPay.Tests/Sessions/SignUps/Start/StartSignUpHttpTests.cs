using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MoniPay.Api;
using MoniPay.Api.Hosting;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Sessions;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Providers;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.Start;

public sealed class StartSignUpHttpTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task Starting_returns_202_with_the_resource_and_queues_one_delivery_before_any_sms()
    {
        string phone = TestPhones.Next();
        using StringContent body = SignUpFlow.StartBody(phone);

        using HttpResponseMessage response = await SignUpFlow.PostStartAsync(Client, body);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(MoniPayMediaTypes.JsonApi, response.Content.Headers.ContentType?.ToString());
        AssertNoStore(response);

        JsonElement document = await ReadSuccessAsync(response);
        JsonElement data = document.GetProperty("data");
        Assert.Equal(SignUpResourceTypes.SignUps, data.GetProperty("type").GetString());
        string id = data.GetProperty("id").GetString()!;
        Assert.True(Guid.TryParse(id, out _));

        JsonElement attributes = data.GetProperty("attributes");
        Assert.Equal(SignUpStatuses.CodePending, attributes.GetProperty("status").GetString());
        Assert.Equal(CodeDeliveryValues.Queued, attributes.GetProperty("codeDelivery").GetString());
        Assert.False(string.IsNullOrWhiteSpace(attributes.GetProperty("signUpToken").GetString()));
        Assert.False(attributes.TryGetProperty("registrationToken", out _));
        Assert.False(string.IsNullOrWhiteSpace(attributes.GetProperty("codeExpiresAt").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(attributes.GetProperty("canResendAt").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(attributes.GetProperty("signUpExpiresAt").GetString()));
        Assert.False(attributes.TryGetProperty("id", out _));

        string self = SignUpRoutes.Group + "/" + id;
        Assert.Equal(self, data.GetProperty("links").GetProperty("self").GetString());
        Assert.Equal(self, document.GetProperty("links").GetProperty("self").GetString());
        Assert.Equal(self, response.Headers.Location?.ToString());

        SignUpId signUpId = new(Guid.Parse(id));
        Assert.Single(await SignUpFlow.NotificationsForAsync(Api, signUpId));

        Assert.Empty(Api.Sms.CallsFor(phone));
        await Api.RunNotificationCycleAsync(Cancellation);
        Assert.Single(Api.Sms.CallsFor(phone));
    }

    [Theory]
    [InlineData("+237699123456")]
    [InlineData("237 699123456")]
    [InlineData("23769912")]
    [InlineData("999699123456")]
    [InlineData("2376991234567890123")]
    public async Task An_invalid_phone_is_422_at_the_phone_pointer(string phone)
    {
        using StringContent body = SignUpFlow.StartBody(phone);

        using HttpResponseMessage response = await SignUpFlow.PostStartAsync(Client, body);

        await AssertValidationAsync(response, StartSignUpPointers.Phone);
    }

    [Theory]
    [InlineData("", SignUpFlow.PrivacyVersion, "termsVersion")]
    [InlineData(SignUpFlow.TermsVersion, "", "privacyVersion")]
    [InlineData("terms-2000-01", SignUpFlow.PrivacyVersion, "termsVersion")]
    [InlineData(SignUpFlow.TermsVersion, "privacy-2000-01", "privacyVersion")]
    public async Task An_outdated_legal_version_is_422_not_409(
        string termsVersion,
        string privacyVersion,
        string expectedMember)
    {
        string phone = TestPhones.Next();
        string json = JsonSerializer.Serialize(new
        {
            data = new
            {
                type = SignUpResourceTypes.SignUps,
                attributes = new { phone, termsVersion, privacyVersion },
            },
        });
        using StringContent body = JsonBody(json);

        using HttpResponseMessage response = await SignUpFlow.PostStartAsync(Client, body);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(MoniPayErrorTypes.Validation.Urn, problem.Type);
        Assert.Contains("/data/attributes/" + expectedMember, PointersOf(problem));
    }

    [Fact]
    public async Task Both_outdated_legal_versions_report_both_pointers()
    {
        using StringContent body = SignUpFlow.StartBody(TestPhones.Next(), "terms-2000-01", "privacy-2000-01");

        using HttpResponseMessage response = await SignUpFlow.PostStartAsync(Client, body);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Contains("/data/attributes/termsVersion", PointersOf(problem));
        Assert.Contains("/data/attributes/privacyVersion", PointersOf(problem));
    }

    [Theory]
    [InlineData("""{"data":{"type":"signups","attributes":{"phone":"237699123456","termsVersion":"x","privacyVersion":"y","extra":1}}}""")]
    [InlineData("""{"data":[]}""")]
    [InlineData("""{"data":null}""")]
    [InlineData("""{"data":{"type":"Signups","attributes":{"phone":"237699123456","termsVersion":"x","privacyVersion":"y"}}}""")]
    public async Task An_invalid_document_is_400(string json)
    {
        using StringContent body = JsonBody(json);

        using HttpResponseMessage response = await SignUpFlow.PostStartAsync(Client, body);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(MoniPayErrorTypes.JsonApiDocumentInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task Malformed_json_is_400()
    {
        using StringContent body = JsonBody("""{"data":""");

        using HttpResponseMessage response = await SignUpFlow.PostStartAsync(Client, body);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(MoniPayErrorTypes.MalformedJson.Urn, problem.Type);
    }

    [Fact]
    public async Task Accepting_only_problem_json_is_406()
    {
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.ProblemJson);
        using StringContent body = SignUpFlow.StartBody(TestPhones.Next());

        using HttpResponseMessage response = await client.PostAsync(SignUpFlow.StartUrl(), body, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.NotAcceptable);
        Assert.Equal(MoniPayErrorTypes.NotAcceptable.Urn, problem.Type);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/vnd.api+json; charset=utf-8")]
    [InlineData("application/vnd.api+json; foo=bar")]
    public async Task An_unsupported_content_type_is_415(string contentType)
    {
        using StringContent body = SignUpFlow.StartBody(TestPhones.Next());
        body.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        using HttpResponseMessage response = await SignUpFlow.PostStartAsync(Client, body);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnsupportedMediaType);
        Assert.Equal(MoniPayErrorTypes.UnsupportedMediaType.Urn, problem.Type);
    }

    [Fact]
    public async Task Restarting_before_the_cooldown_is_429_with_retry_after()
    {
        string phone = TestPhones.Next();
        using (StringContent first = SignUpFlow.StartBody(phone))
        {
            using HttpResponseMessage accepted = await SignUpFlow.PostStartAsync(Client, first);
            Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        }

        using StringContent second = SignUpFlow.StartBody(phone);
        using HttpResponseMessage response = await SignUpFlow.PostStartAsync(Client, second);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.TooManyRequests);
        Assert.Equal(MoniPayErrorTypes.SignUpResendTooSoon.Urn, problem.Type);
        Assert.NotNull(response.Headers.RetryAfter);
    }

    [Fact]
    public async Task Restarting_a_verified_phone_is_409()
    {
        PhoneNumber number = new(TestPhones.Next());
        StartSignUpResult started = await Api.InScopeAsync<StartSignUpHandler, StartSignUpResult>(
            (handler, token) => handler.HandleAsync(
                new StartSignUpCommand(number, Locale.FrenchCameroon, SignUpFlow.TermsVersion, SignUpFlow.PrivacyVersion),
                token));
        await Api.VerifyPhoneAsync(started.SignUpId, await Api.DeliveredCodeAsync(number));

        using StringContent retry = SignUpFlow.StartBody(number.Value);
        using HttpResponseMessage response = await SignUpFlow.PostStartAsync(Client, retry);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(MoniPayErrorTypes.SignUpStateInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task An_ip_budget_exceeded_is_429_with_retry_after()
    {
        using WebApplicationFactory<Program> host = Api.CreateHost(builder =>
            builder.UseSetting(RateLimitOptions.Keys.StartPerHour, "1"));
        using HttpClient client = host.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.Accept);

        using (StringContent first = SignUpFlow.StartBody(TestPhones.Next()))
        {
            first.Headers.ContentType = MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);
            using HttpResponseMessage accepted = await client.PostAsync(
                SignUpFlow.StartUrl(),
                first,
                Cancellation);
            Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        }

        using StringContent second = SignUpFlow.StartBody(TestPhones.Next());
        second.Headers.ContentType = MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);
        using HttpResponseMessage response = await client.PostAsync(
            SignUpFlow.StartUrl(),
            second,
            Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.TooManyRequests);
        Assert.Equal(MoniPayErrorTypes.RateLimited.Urn, problem.Type);
        Assert.NotNull(response.Headers.RetryAfter);
    }

    [Fact]
    public async Task A_queue_failure_is_503_without_a_sign_up()
    {
        PhoneNumber phone = new(TestPhones.Next());
        using WebApplicationFactory<Program> failing = Api.CreateHost(builder =>
        {
            builder.UseTestPorts(Api);
            MoniPayApi.UseNotificationTestSettings(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Api.Time);
                services.AddSingleton<IVerificationCodeSender>(new FailingSender());
            });
        });
        using HttpClient client = failing.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.Accept);
        using StringContent body = SignUpFlow.StartBody(phone.Value);
        body.Headers.ContentType = MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);

        using HttpResponseMessage response = await client.PostAsync(SignUpFlow.StartUrl(), body, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.ServiceUnavailable);
        Assert.Equal(MoniPayErrorTypes.VerificationDeliveryUnavailable.Urn, problem.Type);
        Assert.Equal(0, await Api.CountSignUpsAsync(phone));
    }

    [Theory]
    [InlineData("en", "The phone number format is invalid.")]
    [InlineData("fr", "Le format du numéro de téléphone est invalide.")]
    public async Task Validation_problems_are_localized(string culture, string detail)
    {
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        using StringContent body = SignUpFlow.StartBody("bad");

        using HttpResponseMessage response = await SignUpFlow.PostStartAsync(client, body);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        JsonElement errors = ProblemErrors(problem);
        Assert.Contains(
            errors.EnumerateArray(),
            error => error.GetProperty("detail").GetString() == detail
                && error.GetProperty("pointer").GetString() == StartSignUpPointers.Phone);
    }

    private static void AssertNoStore(HttpResponseMessage response)
    {
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.True(response.Headers.TryGetValues("Pragma", out IEnumerable<string>? pragma));
        Assert.Equal("no-cache", Assert.Single(pragma!));
    }

    private static async Task<JsonElement> ReadSuccessAsync(HttpResponseMessage response)
    {
        JsonElement document = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(JsonApiVersion.Current, document.GetProperty("jsonapi").GetProperty("version").GetString());

        return document;
    }

    private static async Task AssertValidationAsync(HttpResponseMessage response, string pointer)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(MoniPayErrorTypes.Validation.Urn, problem.Type);
        Assert.Contains(pointer, PointersOf(problem));
    }

    private static IReadOnlyList<string> PointersOf(Microsoft.AspNetCore.Mvc.ProblemDetails problem) =>
        ProblemErrors(problem).EnumerateArray().Select(error => error.GetProperty("pointer").GetString()!).ToArray();

    private static JsonElement ProblemErrors(Microsoft.AspNetCore.Mvc.ProblemDetails problem)
    {
        problem.Extensions.TryGetValue("errors", out object? errors);

        return (JsonElement)errors!;
    }

    private static StringContent JsonBody(string json)
    {
        StringContent body = new(json, Encoding.UTF8);
        body.Headers.ContentType = MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);

        return body;
    }

    private sealed class FailingSender : IVerificationCodeSender
    {
        public Task EnqueueAsync(VerificationCodeMessage message, CancellationToken cancellationToken) =>
            throw new ProviderUnavailableException("sms");

        public Task<CodeDeliveryState?> GetLatestDeliveryAsync(SignUpId signUpId, CancellationToken cancellationToken) =>
            Task.FromResult<CodeDeliveryState?>(null);
    }
}
