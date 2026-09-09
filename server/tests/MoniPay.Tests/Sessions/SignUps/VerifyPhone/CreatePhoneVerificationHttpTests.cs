using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Features.SignUps.VerifyPhone;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.VerifyPhone;

public sealed class CreatePhoneVerificationHttpTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private const int MaximumAttempts = 3;
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan SignUpLifetime = TimeSpan.FromMinutes(15);

    [Fact]
    public async Task The_right_code_returns_200_the_verified_state_and_the_registration_token()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);

        using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            await Api.DeliveredCodeAsync(phone));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());

        JsonElement document = await JsonApiAssertions.ReadJsonApiAsync(
            response,
            SignUpResourceTypes.SignUps,
            started.SignUpId.Value.ToString());
        JsonElement attributes = document.GetProperty("data").GetProperty("attributes");
        Assert.Equal(JsonApiAssertions.Wire(SignUpStatusValue.PhoneVerified), attributes.GetProperty("status").GetString());
        string registrationToken = attributes.GetProperty("registrationToken").GetString()
            ?? throw new Xunit.Sdk.XunitException("The registration token was not a JSON string.");
        Assert.NotEmpty(registrationToken);
        Assert.Equal(started.SignUpExpiresAt, attributes.GetProperty("signUpExpiresAt").GetDateTimeOffset());
        Assert.False(attributes.TryGetProperty("signUpToken", out _));
        Assert.Equal(
            SignUpResources.Self(started.SignUpId),
            document.GetProperty("data").GetProperty("links").GetProperty("self").GetString());

        SignUp row = await Api.ReadSignUpRowAsync(started.SignUpId);
        Assert.Equal(SignUpStatus.PhoneVerified, row.Status);
        Assert.Null(row.SignUpTokenDigest);
        Assert.Empty(await Api.RowsContainingAsync(registrationToken));
    }

    [Fact]
    public async Task The_sign_up_credential_stops_working_once_the_phone_is_verified()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        await SignUpFlow.PostVerifyAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            await Api.DeliveredCodeAsync(phone));

        using HttpResponseMessage read = await SignUpFlow.GetSignUpHttpAsync(
            Client,
            started.SignUpId,
            SessionsSchemes.SignUp,
            started.SignUpToken);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await read.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SignUpTokenInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task A_wrong_code_is_422_at_its_pointer_and_the_attempt_is_kept()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);

        using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            SignUpFlow.Wrong(await Api.DeliveredCodeAsync(phone)));

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(MoniPayErrorTypes.VerificationCodeInvalid.Urn, problem.Type);
        Assert.Equal(
            CreatePhoneVerificationPointers.VerificationCode,
            Assert.Single(problem.Pointers()));
        Assert.Equal(1, (await Api.ReadSignUpRowAsync(started.SignUpId)).FailedAttempts);
    }

    [Fact]
    public async Task The_last_permitted_failure_locks_the_sign_up_with_a_retry_delay()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string wrong = SignUpFlow.Wrong(await Api.DeliveredCodeAsync(phone));
        for (int attempt = 1; attempt < MaximumAttempts; attempt++)
        {
            using HttpResponseMessage mismatch = await SignUpFlow.PostVerifyAsync(
                Client,
                started.SignUpId,
                started.SignUpToken,
                wrong);
            await mismatch.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        }

        using HttpResponseMessage locking = await SignUpFlow.PostVerifyAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            wrong);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await locking.ReadProblemAsync(HttpStatusCode.TooManyRequests);
        Assert.Equal(MoniPayErrorTypes.SignUpAttemptLimit.Urn, problem.Type);
        Assert.NotNull(locking.Headers.RetryAfter);
        SignUp row = await Api.ReadSignUpRowAsync(started.SignUpId);
        Assert.Equal(SignUpStatus.Locked, row.Status);
        Assert.Equal(MaximumAttempts, row.FailedAttempts);
    }

    [Fact]
    public async Task An_expired_code_is_410_and_a_resend_still_works()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string code = await Api.DeliveredCodeAsync(phone);
        Api.Time.Advance(CodeLifetime + TimeSpan.FromSeconds(1));

        using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            code);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Gone);
        Assert.Equal(MoniPayErrorTypes.VerificationCodeExpired.Urn, problem.Type);
        Assert.Equal(SignUpStatus.CodePending, (await Api.ReadSignUpRowAsync(started.SignUpId)).Status);

        using HttpResponseMessage resent = await SignUpFlow.PostResendAsync(
            Client,
            started.SignUpId,
            started.SignUpToken);
        Assert.Equal(HttpStatusCode.Accepted, resent.StatusCode);
    }

    [Fact]
    public async Task A_code_that_is_not_the_expected_digits_is_422_and_spends_no_attempt()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);

        using HttpResponseMessage tooShort = await SignUpFlow.PostVerifyAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            "12345");

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await tooShort.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(MoniPayErrorTypes.Validation.Urn, problem.Type);
        Assert.Equal(
            CreatePhoneVerificationPointers.VerificationCode,
            Assert.Single(problem.Pointers()));
        Assert.Equal(0, (await Api.ReadSignUpRowAsync(started.SignUpId)).FailedAttempts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("1234 6")]
    [InlineData("12345a")]
    [InlineData("12٣٤٥")]
    public async Task A_code_outside_the_digit_rule_is_422_at_its_pointer(string? code)
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));

        using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            code);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(
            CreatePhoneVerificationPointers.VerificationCode,
            Assert.Single(problem.Pointers()));
        Assert.Equal(0, (await Api.ReadSignUpRowAsync(started.SignUpId)).FailedAttempts);
    }

    [Fact]
    public async Task A_code_that_is_not_a_string_is_a_document_failure()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        using StringContent body = new(
            $"{{\"data\":{{\"type\":\"phone-verifications\",\"attributes\":{{\"verificationCode\":123456}},\"relationships\":{{\"signUp\":{{\"data\":{{\"type\":\"signups\",\"id\":\"{started.SignUpId}\"}}}}}}}}}}",
            Encoding.UTF8);
        body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);

        using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            "",
            body);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(MoniPayErrorTypes.JsonApiDocumentInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task A_relationship_that_names_another_sign_up_is_409()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string code = await Api.DeliveredCodeAsync(phone);

        using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            code,
            SignUpFlow.VerifyBody(SignUpId.New(), code));

        Microsoft.AspNetCore.Mvc.ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(MoniPayErrorTypes.ResourceIdentityMismatch.Urn, problem.Type);
        Assert.Equal(SignUpStatus.CodePending, (await Api.ReadSignUpRowAsync(started.SignUpId)).Status);
    }

    [Theory]
    [InlineData("""{"data":{"type":"phone-verifications","attributes":{"verificationCode":"123456"}}}""")]
    [InlineData("""{"data":{"type":"phone-verifications","attributes":{"verificationCode":"123456"},"relationships":{}}}""")]
    [InlineData("""{"data":{"type":"phone-verifications","attributes":{"verificationCode":"123456"},"relationships":null}}""")]
    [InlineData("""{"data":{"type":"phone-verifications","attributes":{"verificationCode":"123456"},"relationships":{"signUp":null}}}""")]
    [InlineData("""{"data":{"type":"phone-verifications","attributes":{"verificationCode":"123456"},"relationships":{"signUp":{}}}}""")]
    [InlineData("""{"data":{"type":"phone-verifications","attributes":{"verificationCode":"123456"},"relationships":{"signUp":[]}}}""")]
    [InlineData("""{"data":{"type":"phone-verifications","attributes":{"verificationCode":"123456"},"relationships":{"signUp":{"data":null}}}}""")]
    [InlineData("""{"data":{"type":"phone-verifications","attributes":{"verificationCode":"123456"},"relationships":{"signUp":{"data":{"type":"verifications","id":"1207158c-15fc-446d-a28a-702c564332ef"}}}}}""")]
    [InlineData("""{"data":{"type":"phone-verifications","attributes":{"verificationCode":"123456"},"relationships":{"signUp":{"data":{"type":"signups","id":"not-a-guid"}}}}}""")]
    public async Task A_relationship_the_route_cannot_bind_to_is_400(string json)
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        using StringContent body = new(json, Encoding.UTF8);
        body.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(MoniPayMediaTypes.JsonApi);

        using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            "",
            body);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(MoniPayErrorTypes.JsonApiDocumentInvalid.Urn, problem.Type);
        Assert.Equal(0, (await Api.ReadSignUpRowAsync(started.SignUpId)).FailedAttempts);
    }

    [Fact]
    public async Task A_credential_for_another_sign_up_is_401_whatever_the_body_says()
    {
        StartSignUpResult other = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        StartSignUpResult mine = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        string code = "123456";

        using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
            Client,
            mine.SignUpId,
            other.SignUpToken,
            code,
            SignUpFlow.VerifyBody(other.SignUpId, code));

        await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_phone_a_user_already_owns_is_409_and_its_sign_up_closed()
    {
        PhoneNumber phone = new(TestPhones.Next());
        await Api.RegisterUserAsync(phone);

        try
        {
            StartSignUpResult started = await Api.StartSignUpAsync(phone);

            using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
                Client,
                started.SignUpId,
                started.SignUpToken,
                await Api.DeliveredCodeAsync(phone));

            Microsoft.AspNetCore.Mvc.ProblemDetails problem =
                await response.ReadProblemAsync(HttpStatusCode.Conflict);
            Assert.Equal(MoniPayErrorTypes.PhoneAlreadyRegistered.Urn, problem.Type);

            SignUp row = await Api.ReadSignUpRowAsync(started.SignUpId);
            Assert.Equal(SignUpStatus.Expired, row.Status);
            Assert.Null(row.RegistrationTokenDigest);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task A_wrong_code_does_not_disclose_that_the_phone_is_registered()
    {
        PhoneNumber phone = new(TestPhones.Next());
        await Api.RegisterUserAsync(phone);
        StartSignUpResult started = await Api.StartSignUpAsync(phone);

        try
        {
            using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
                Client,
                started.SignUpId,
                started.SignUpToken,
                SignUpFlow.Wrong(await Api.DeliveredCodeAsync(phone)));

            Microsoft.AspNetCore.Mvc.ProblemDetails problem =
                await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
            Assert.Equal(MoniPayErrorTypes.VerificationCodeInvalid.Urn, problem.Type);
            Assert.Equal(SignUpStatus.CodePending, (await Api.ReadSignUpRowAsync(started.SignUpId)).Status);
        }
        finally
        {
            await Api.CleanUsersAsync();
        }
    }

    [Fact]
    public async Task Eight_parallel_correct_verifications_issue_one_registration_token()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string code = await Api.DeliveredCodeAsync(phone);

        HttpResponseMessage[] responses = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => SignUpFlow.PostVerifyAsync(
                Client,
                started.SignUpId,
                started.SignUpToken,
                code)));

        try
        {
            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
            Assert.All(
                responses.Where(response => response.StatusCode != HttpStatusCode.OK),
                response => Assert.True(
                    response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Conflict,
                    $"Unexpected status {response.StatusCode}."));

            SignUp row = await Api.ReadSignUpRowAsync(started.SignUpId);
            Assert.Equal(SignUpStatus.PhoneVerified, row.Status);
            Assert.NotNull(row.RegistrationTokenDigest);
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task Eight_parallel_wrong_codes_never_exceed_the_attempt_budget()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string wrong = SignUpFlow.Wrong(await Api.DeliveredCodeAsync(phone));

        HttpResponseMessage[] responses = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => SignUpFlow.PostVerifyAsync(
                Client,
                started.SignUpId,
                started.SignUpToken,
                wrong)));

        try
        {
            Assert.All(responses, response => Assert.NotEqual(HttpStatusCode.OK, response.StatusCode));
            Assert.Equal(MaximumAttempts, (await Api.ReadSignUpRowAsync(started.SignUpId)).FailedAttempts);
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task An_expired_workflow_is_refused_at_authentication()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string code = await Api.DeliveredCodeAsync(phone);
        Api.Time.Advance(SignUpLifetime);

        using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
            Client,
            started.SignUpId,
            started.SignUpToken,
            code);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SignUpTokenInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task Accepting_only_problem_json_is_406()
    {
        StartSignUpResult started = await Api.StartSignUpAsync(new PhoneNumber(TestPhones.Next()));
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.ProblemJson);

        using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
            client,
            started.SignUpId,
            started.SignUpToken,
            "123456");

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.NotAcceptable);
        Assert.Equal(MoniPayErrorTypes.NotAcceptable.Urn, problem.Type);
    }

    [Theory]
    [InlineData("en", "The verification code is incorrect.")]
    [InlineData("fr", "Le code de vérification est incorrect.")]
    public async Task Problems_are_localized(string culture, string title)
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string wrong = SignUpFlow.Wrong(await Api.DeliveredCodeAsync(phone));
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);

        using HttpResponseMessage response = await SignUpFlow.PostVerifyAsync(
            client,
            started.SignUpId,
            started.SignUpToken,
            wrong);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(MoniPayErrorTypes.VerificationCodeInvalid.Urn, problem.Type);
        Assert.Equal(title, problem.Title);
    }

}
