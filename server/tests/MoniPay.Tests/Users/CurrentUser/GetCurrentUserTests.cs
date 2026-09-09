using System.Net;
using System.Text;
using System.Text.Json;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Security;
using MoniPay.Tests.Support;
using MoniPay.Users.Features.CurrentUser;
using MoniPay.Users.Features.Registration;
using Xunit;

namespace MoniPay.Tests.Users.CurrentUser;

public sealed class GetCurrentUserTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task Completing_then_reading_returns_the_stored_profile()
    {
        (Profile profile, string phone, string userId, string accessToken) = await CompleteViaHttpAsync();

        int logsBefore = Api.Logs.Entries.Count;
        using HttpResponseMessage response = await SignUpFlow.GetCurrentUserAsync(Client, accessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MoniPayMediaTypes.JsonApi, response.Content.Headers.ContentType?.ToString());
        response.AssertNoStore();

        JsonElement document = await JsonApiAssertions.ReadJsonApiAsync(
            response,
            UserResourceTypes.Users,
            userId);
        JsonElement attributes = document.GetProperty("data").GetProperty("attributes");
        Assert.Equal(
            ["createdAt", "email", "firstName", "lastName", "locale", "phone"],
            attributes.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        Assert.Equal(profile.FirstName, attributes.GetProperty("firstName").GetString());
        Assert.Equal(profile.LastName, attributes.GetProperty("lastName").GetString());
        Assert.Equal(profile.Email, attributes.GetProperty("email").GetString());
        Assert.Equal(phone, attributes.GetProperty("phone").GetString());
        Assert.Equal(Locale.FrenchTag, attributes.GetProperty("locale").GetString());
        Assert.Equal(TimeSpan.Zero, attributes.GetProperty("createdAt").GetDateTimeOffset().Offset);
        Assert.False(attributes.TryGetProperty("id", out _));
        Assert.Equal(MoniPayRoutes.CurrentUser, document.GetProperty("data").GetProperty("links").GetProperty("self").GetString());

        string raw = await response.Content.ReadAsStringAsync(Cancellation);
        Assert.DoesNotContain("ciphertext", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lookupHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("consent", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("kyc", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessToken", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("refreshToken", raw, StringComparison.Ordinal);

        Assert.DoesNotContain(
            Api.Logs.Entries.Skip(logsBefore),
            entry => entry.Message.Contains(profile.Email, StringComparison.Ordinal)
                || entry.Message.Contains(phone, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Two_users_each_read_only_their_own_profile()
    {
        (OpenedSession first, RegisteredUser firstUser, PhoneNumber firstPhone, string firstEmail) = await RegisteredSessionAsync();
        (OpenedSession second, RegisteredUser secondUser, PhoneNumber secondPhone, string secondEmail) = await RegisteredSessionAsync();

        using HttpResponseMessage firstResponse = await SignUpFlow.GetCurrentUserAsync(Client, AccessToken(first));
        using HttpResponseMessage secondResponse = await SignUpFlow.GetCurrentUserAsync(Client, AccessToken(second));

        JsonElement firstDocument = await JsonApiAssertions.ReadJsonApiAsync(
            firstResponse,
            UserResourceTypes.Users,
            firstUser.Id.Value.ToString());
        JsonElement secondDocument = await JsonApiAssertions.ReadJsonApiAsync(
            secondResponse,
            UserResourceTypes.Users,
            secondUser.Id.Value.ToString());
        Assert.Equal(firstPhone.Value, firstDocument.GetProperty("data").GetProperty("attributes").GetProperty("phone").GetString());
        Assert.Equal(firstEmail, firstDocument.GetProperty("data").GetProperty("attributes").GetProperty("email").GetString());
        Assert.Equal(secondPhone.Value, secondDocument.GetProperty("data").GetProperty("attributes").GetProperty("phone").GetString());
        Assert.Equal(secondEmail, secondDocument.GetProperty("data").GetProperty("attributes").GetProperty("email").GetString());

        using HttpRequestMessage forged = new(
            HttpMethod.Get,
            $"{SignUpFlow.CurrentUserUrl()}?userId={secondUser.Id}&sub={secondUser.Id}&sessionId={second.Session.SessionId}");
        forged.Headers.Authorization = new(MoniPayHeaders.Bearer, AccessToken(first));
        using HttpResponseMessage forgedResponse = await Client.SendAsync(forged, Cancellation);
        JsonElement forgedDocument = await JsonApiAssertions.ReadJsonApiAsync(
            forgedResponse,
            UserResourceTypes.Users,
            firstUser.Id.Value.ToString());
        Assert.Equal(firstPhone.Value, forgedDocument.GetProperty("data").GetProperty("attributes").GetProperty("phone").GetString());
    }

    [Fact]
    public async Task Stored_names_email_and_phone_are_returned_as_stored()
    {
        PersonName firstName = new("Anne-Marie");
        PersonName lastName = new("Ngo'o Nyobè");
        string displayEmail = $"Marie.NGO.{Guid.NewGuid():N}@example.com";
        EmailAddress email = new(displayEmail);
        PhoneNumber phone = new(TestPhones.Next());
        RegisteredUser registered = await Api.InScopeAsync<RegisterUserHandler, RegisteredUser>(
            (handler, cancellationToken) => handler.HandleAsync(
                new RegisterUserCommand(
                    SignUpId.New(),
                    UserId.New(),
                    phone,
                    firstName,
                    lastName,
                    email,
                    Locale.FrenchCameroon,
                    SignUpFlow.TermsVersion,
                    SignUpFlow.PrivacyVersion,
                    new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero)),
                cancellationToken));
        OpenedSession session = await Api.CreateSessionAsync(registered.Id);

        using HttpResponseMessage response = await SignUpFlow.GetCurrentUserAsync(Client, AccessToken(session));

        JsonElement attributes = (await JsonApiAssertions.ReadJsonApiAsync(response))
            .GetProperty("data").GetProperty("attributes");
        Assert.Equal("Anne-Marie", attributes.GetProperty("firstName").GetString());
        Assert.Equal("Ngo'o Nyobè", attributes.GetProperty("lastName").GetString());
        Assert.Equal(displayEmail, attributes.GetProperty("email").GetString());
        Assert.Equal(phone.Value, attributes.GetProperty("phone").GetString());
    }

    [Fact]
    public async Task Stored_locale_survives_another_accept_language()
    {
        (OpenedSession session, _, _, _) = await RegisteredSessionAsync();
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(Locale.EnglishTag);

        using HttpResponseMessage response = await SignUpFlow.GetCurrentUserAsync(client, AccessToken(session));
        JsonElement attributes = (await JsonApiAssertions.ReadJsonApiAsync(response))
            .GetProperty("data").GetProperty("attributes");
        Assert.Equal(Locale.FrenchCameroonTag, attributes.GetProperty("locale").GetString());

        using HttpResponseMessage reread = await SignUpFlow.GetCurrentUserAsync(Client, AccessToken(session));
        JsonElement rereadAttributes = (await JsonApiAssertions.ReadJsonApiAsync(reread))
            .GetProperty("data").GetProperty("attributes");
        Assert.Equal(Locale.FrenchCameroonTag, rereadAttributes.GetProperty("locale").GetString());
    }

    [Fact]
    public async Task Repeated_reads_leave_the_profile_unchanged()
    {
        (OpenedSession session, RegisteredUser user, _, _) = await RegisteredSessionAsync();

        using HttpResponseMessage first = await SignUpFlow.GetCurrentUserAsync(Client, AccessToken(session));
        using HttpResponseMessage second = await SignUpFlow.GetCurrentUserAsync(Client, AccessToken(session));

        JsonElement firstData = (await JsonApiAssertions.ReadJsonApiAsync(first)).GetProperty("data");
        JsonElement secondData = (await JsonApiAssertions.ReadJsonApiAsync(second)).GetProperty("data");

        Assert.Equal(user.Id.Value.ToString(), secondData.GetProperty("id").GetString());
        Assert.True(JsonElement.DeepEquals(firstData, secondData));
    }

    [Fact]
    public async Task A_missing_credential_is_401_with_the_bearer_challenge()
    {
        using HttpResponseMessage response = await Client.GetAsync(SignUpFlow.CurrentUserUrl(), Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal(MoniPayHeaders.Bearer, response.Headers.WwwAuthenticate.ToString());
        Assert.DoesNotContain("firstName", await response.Content.ReadAsStringAsync(Cancellation), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_expired_credential_is_401()
    {
        (OpenedSession session, _, _, _) = await RegisteredSessionAsync();
        string expired = TestTokens.Bearer(
            session.Session.UserId,
            session.Session.SessionId,
            expires: DateTimeOffset.UtcNow - TimeSpan.FromMinutes(1));

        using HttpResponseMessage response = await SignUpFlow.GetCurrentUserAsync(Client, expired);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task A_revoked_session_is_401()
    {
        (OpenedSession session, _, _, _) = await RegisteredSessionAsync();
        await SignUpFlow.RevokeCurrentSessionAsync(Client, AccessToken(session));

        using HttpResponseMessage response = await SignUpFlow.GetCurrentUserAsync(Client, AccessToken(session));

        await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_registration_credential_does_not_open_the_route()
    {
        PhoneNumber phone = new(TestPhones.Next());
        StartSignUpResult started = await Api.StartSignUpAsync(phone);
        string registration = (await Api.VerifyPhoneAsync(started.SignUpId, await Api.DeliveredCodeAsync(phone))).RegistrationToken;

        using HttpRequestMessage request = new(HttpMethod.Get, SignUpFlow.CurrentUserUrl());
        request.Headers.Authorization = new(SessionsSchemes.Registration, registration);
        using HttpResponseMessage response = await Client.SendAsync(request, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
    }

    [Theory]
    [InlineData("not-a-guid", true)]
    [InlineData(null, true)]
    [InlineData("not-a-guid", false)]
    [InlineData(null, false)]
    public async Task A_malformed_identity_is_401_never_500(string? subject, bool keepSession)
    {
        (OpenedSession session, _, _, _) = await RegisteredSessionAsync();
        Dictionary<string, object> claims = new()
        {
            [MoniPayClaimTypes.SessionId] = keepSession
                ? session.Session.SessionId.ToString()
                : Guid.CreateVersion7().ToString(),
        };
        if (subject is not null)
        {
            claims[MoniPayClaimTypes.Subject] = subject;
        }

        using HttpResponseMessage response = await SignUpFlow.GetCurrentUserAsync(Client, TestTokens.BearerWithClaims(claims));

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
    }

    [Fact]
    public async Task A_session_for_another_user_is_401()
    {
        (OpenedSession first, _, _, _) = await RegisteredSessionAsync();
        (OpenedSession second, _, _, _) = await RegisteredSessionAsync();
        string mismatched = TestTokens.Bearer(first.Session.UserId, second.Session.SessionId);

        using HttpResponseMessage response = await SignUpFlow.GetCurrentUserAsync(Client, mismatched);

        await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_user_missing_after_authentication_is_401_with_the_bearer_challenge()
    {
        OpenedSession orphan = await Api.CreateSessionAsync(UserId.New());

        using HttpResponseMessage response = await SignUpFlow.GetCurrentUserAsync(Client, AccessToken(orphan));

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal(MoniPayHeaders.Bearer, response.Headers.WwwAuthenticate.ToString());
        string raw = await response.Content.ReadAsStringAsync(Cancellation);
        Assert.DoesNotContain("firstName", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_read_carrying_a_body_is_415_before_it_is_read()
    {
        (OpenedSession session, _, _, _) = await RegisteredSessionAsync();

        using HttpRequestMessage request = new(HttpMethod.Get, SignUpFlow.CurrentUserUrl())
        {
            Content = new StringContent(
                """{"data":{"type":"users","attributes":{"firstName":"Mallory"}}}""",
                Encoding.UTF8,
                MoniPayMediaTypes.JsonApi),
        };
        request.Headers.Authorization = new(MoniPayHeaders.Bearer, AccessToken(session));

        using HttpResponseMessage response = await Client.SendAsync(request, Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.UnsupportedMediaType);
        Assert.Equal(MoniPayErrorTypes.UnsupportedMediaType.Urn, problem.Type);
        Assert.DoesNotContain("Mallory", await response.Content.ReadAsStringAsync(Cancellation), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_corrupt_contact_is_500_without_leaking_personal_data()
    {
        PhoneNumber phone = new(TestPhones.Next());
        string email = $"corrupt.marie.{Guid.NewGuid():N}@example.com";
        RegisteredUser registered = await Api.RegisterUserAsync(phone, email);
        OpenedSession session = await Api.CreateSessionAsync(registered.Id);
        await Api.QueryAsync(
            $"UPDATE users SET first_name_ciphertext = 'corrupt' WHERE id = '{registered.Id.Value}';",
            reader => 0);
        int logsBefore = Api.Logs.Entries.Count;

        using HttpResponseMessage response = await SignUpFlow.GetCurrentUserAsync(Client, AccessToken(session));

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.InternalServerError);
        Assert.Equal(MoniPayErrorTypes.Internal.Urn, problem.Type);
        string raw = await response.Content.ReadAsStringAsync(Cancellation);
        Assert.DoesNotContain(phone.Value, raw, StringComparison.Ordinal);
        Assert.DoesNotContain(email, raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("corrupt", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            Api.Logs.Entries.Skip(logsBefore),
            entry => entry.Message.Contains(phone.Value, StringComparison.Ordinal)
                || entry.Message.Contains(email, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Accepting_only_problem_json_is_406()
    {
        (OpenedSession session, _, _, _) = await RegisteredSessionAsync();
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd(MoniPayMediaTypes.ProblemJson);

        using HttpResponseMessage response = await SignUpFlow.GetCurrentUserAsync(client, AccessToken(session));

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.NotAcceptable);
        Assert.Equal(MoniPayErrorTypes.NotAcceptable.Urn, problem.Type);
    }

    [Theory]
    [InlineData("en", "The session token is invalid or expired.")]
    [InlineData("fr", "Le jeton de session est invalide ou expiré.")]
    public async Task Authentication_problems_are_localized(string culture, string title)
    {
        using HttpClient client = Api.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);

        using HttpResponseMessage response = await client.GetAsync(SignUpFlow.CurrentUserUrl(), Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal(title, problem.Title);
    }

    [Fact]
    public async Task A_problem_without_a_culture_is_french()
    {
        using HttpResponseMessage response = await Client.GetAsync(SignUpFlow.CurrentUserUrl(), Cancellation);

        Microsoft.AspNetCore.Mvc.ProblemDetails problem =
            await response.ReadProblemAsync(HttpStatusCode.Unauthorized);
        Assert.Equal("Le jeton de session est invalide ou expiré.", problem.Title);
    }

    private async Task<(Profile Profile, string Phone, string UserId, string AccessToken)> CompleteViaHttpAsync()
    {
        StartedAndVerified verified = await Api.StartAndVerifyAsync(Client);
        Profile profile = Profile.Sample();
        using HttpResponseMessage completed = await CompletionFlow.PostCompleteAsync(
            Client,
            verified.SignUpId,
            verified.RegistrationToken,
            profile);
        JsonElement session = await completed.ReadJsonApiAsync();
        string accessToken = session
            .GetProperty("data").GetProperty("attributes").GetProperty("accessToken").GetString()
            ?? throw new Xunit.Sdk.XunitException("The completion carried no access token.");
        string userId = session
            .GetProperty("data").GetProperty("relationships").GetProperty("user").GetProperty("data").GetProperty("id").GetString()
            ?? throw new Xunit.Sdk.XunitException("The completion carried no user id.");

        return (profile, verified.Phone.Value, userId, accessToken);
    }

    private async Task<(OpenedSession Session, RegisteredUser User, PhoneNumber Phone, string Email)> RegisteredSessionAsync()
    {
        PhoneNumber phone = new(TestPhones.Next());
        string email = $"currentuser.{Guid.NewGuid():N}@example.com";
        RegisteredUser user = await Api.RegisterUserAsync(phone, email);
        OpenedSession session = await Api.CreateSessionAsync(user.Id);

        return (session, user, phone, email);
    }

    private static string AccessToken(OpenedSession session) =>
        TestTokens.Bearer(session.Session.UserId, session.Session.SessionId);
}
