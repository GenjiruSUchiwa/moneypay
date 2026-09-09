using System.Net;
using System.Text;
using System.Text.Json;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Features.SignUps.VerifyPhone;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Sessions.SignUps.VerifyPhone;

public sealed class CreatePhoneVerificationHttpTests(MoniPayApi api) : MoniPayApiTest(api)
{
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
}
