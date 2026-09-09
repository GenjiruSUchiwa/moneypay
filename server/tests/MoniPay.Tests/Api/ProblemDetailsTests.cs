using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MoniPay.Api.Errors;
using MoniPay.Api.Hosting;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Tests.Fakes;
using MoniPay.Tests.Support;
using MoniPay.Wallet.Endpoints;
using Xunit;

namespace MoniPay.Tests.Api;

public sealed class ProblemDetailsTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private const string ErrorProbe = "/test/errors/";

    [Fact]
    public async Task A_validation_exception_becomes_a_422_with_one_item_per_failure()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}validation", Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);

        Assert.Equal(MoniPayErrorTypes.Validation.Urn, problem.Type);
        IReadOnlyList<(string Detail, string Pointer)> errors = Errors(problem);
        Assert.Equal(3, errors.Count);
        Assert.Equal("/data/attributes/phone", errors[0].Pointer);
        Assert.Equal("/data/attributes/name~1first", errors[1].Pointer);
        Assert.Equal("/data/attributes/email~0local", errors[2].Pointer);
        Assert.All(errors, error => Assert.False(string.IsNullOrWhiteSpace(error.Detail)));
    }

    [Fact]
    public async Task A_refusal_keeps_its_type_status_retry_delay_and_pointer_items()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}refusal", Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.TooManyRequests);

        Assert.Equal(MoniPayErrorTypes.RateLimited.Urn, problem.Type);
        Assert.Equal("2", response.Headers.RetryAfter?.ToString());
        (string detail, string pointer) = Assert.Single(Errors(problem));
        Assert.Equal("/data/attributes/phone", pointer);
        Assert.False(string.IsNullOrWhiteSpace(detail));
    }

    [Fact]
    public async Task A_refusal_without_pointers_omits_the_errors_member()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}refusal-no-pointers", Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);

        Assert.Equal(MoniPayErrorTypes.SignUpStateInvalid.Urn, problem.Type);
        Assert.False(problem.Extensions.ContainsKey("errors"));
    }

    [Fact]
    public async Task A_provider_unavailable_exception_becomes_a_503()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}provider", Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.ServiceUnavailable);

        Assert.Equal(MoniPayErrorTypes.VerificationDeliveryUnavailable.Urn, problem.Type);
        Assert.Null(response.Headers.RetryAfter);
    }

    [Fact]
    public async Task A_lost_update_becomes_a_409()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}concurrency", Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);

        Assert.Equal(MoniPayErrorTypes.ConcurrentModification.Urn, problem.Type);
    }

    [Fact]
    public async Task An_unhandled_exception_becomes_a_500_without_detail_or_errors()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}unhandled", Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.InternalServerError);

        Assert.Equal(MoniPayErrorTypes.Internal.Urn, problem.Type);
        Assert.Null(problem.Detail);
        Assert.False(problem.Extensions.ContainsKey("errors"));
    }

    [Fact]
    public async Task An_application_json_exception_is_not_malformed_json()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}json", Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.InternalServerError);

        Assert.Equal(MoniPayErrorTypes.Internal.Urn, problem.Type);
    }

    [Fact]
    public async Task A_cancellation_the_client_did_not_cause_is_a_500()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}cancelled", Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.InternalServerError);

        Assert.Equal(MoniPayErrorTypes.Internal.Urn, problem.Type);
        Assert.Null(problem.Detail);
    }

    [Fact]
    public async Task An_unknown_problem_code_falls_back_to_the_code_as_title()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}unknown", Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);

        Assert.Equal("urn:monipay:error:mystery", problem.Type);
        Assert.Equal("mystery", problem.Title);
    }

    [Fact]
    public async Task An_explicit_results_problem_keeps_its_title_and_gains_the_writer_fields()
    {
        Client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(Locale.EnglishTag);
        Uri wallet = new($"{WalletRoutes.Group}?{WalletRoutes.CurrencyQuery}=USD", UriKind.Relative);

        using HttpResponseMessage response = await Client.GetAsync(wallet, Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest, requireNoStore: false);

        Assert.Equal(MoniPayErrorTypes.BadRequest.Urn, problem.Type);
        Assert.Equal("Only the XAF wallet is available.", problem.Title);
    }

    [Fact]
    public async Task A_bearer_challenge_names_the_session_credential_and_keeps_the_header()
    {
        using HttpResponseMessage response = await Client.GetAsync("/test/secure", Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.Unauthorized, requireNoStore: false);

        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Urn, problem.Type);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task An_unknown_route_gets_the_not_found_type()
    {
        using HttpResponseMessage response = await Client.GetAsync("/test/no-such-route", Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.NotFound, requireNoStore: false);

        Assert.Equal(MoniPayErrorTypes.NotFound.Urn, problem.Type);
    }

    [Fact]
    public async Task A_wrong_method_gets_the_method_not_allowed_type_and_keeps_allow()
    {
        using HttpResponseMessage response = await Client.PostAsync("/test/no-store", content: null, Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.MethodNotAllowed, requireNoStore: false);

        Assert.Equal(MoniPayErrorTypes.MethodNotAllowed.Urn, problem.Type);
        Assert.Contains("GET", response.Content.Headers.Allow);
    }

    [Fact]
    public async Task The_instance_is_the_request_path_without_the_query_string()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}refusal?trace=abc", Cancellation);

        ProblemDetails problem = await response.ReadProblemAsync(HttpStatusCode.TooManyRequests);

        Assert.Equal($"{ErrorProbe}refusal", problem.Instance);
    }

    [Fact]
    public async Task The_error_content_type_has_no_charset()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}unhandled", Cancellation);

        Assert.Equal(
            MoniPayMediaTypes.ProblemJson,
            response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task A_problem_never_carries_the_exception_message_or_provider_payload()
    {
        using HttpResponseMessage provider = await Client.GetAsync($"{ErrorProbe}provider", Cancellation);
        string providerBody = await provider.Content.ReadAsStringAsync(Cancellation);
        Assert.DoesNotContain("campay", providerBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("E123", providerBody, StringComparison.Ordinal);

        using HttpResponseMessage unhandled = await Client.GetAsync($"{ErrorProbe}unhandled", Cancellation);
        string unhandledBody = await unhandled.Content.ReadAsStringAsync(Cancellation);
        Assert.DoesNotContain("boom", unhandledBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_mapped_failure_writes_one_outcome_event()
    {
        int before = Api.Logs.Entries.Count;

        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}validation", Cancellation);
        await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);

        RecordingLoggerProvider.LogEntry entry = Assert.Single(
            Api.Logs.Entries.Skip(before),
            IsOutcomeEvent);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("phone-format-invalid", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_outcome_event_names_the_endpoint_and_not_the_request_path()
    {
        int before = Api.Logs.Entries.Count;

        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}validation", Cancellation);
        await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);

        RecordingLoggerProvider.LogEntry entry = Assert.Single(
            Api.Logs.Entries.Skip(before),
            IsOutcomeEvent);

        Assert.Equal(TestProbes.ErrorProbeName, Route(entry));
        Assert.DoesNotContain($"{ErrorProbe}validation", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unhandled_exception_writes_one_error_event()
    {
        int before = Api.Logs.Entries.Count;

        using HttpResponseMessage response = await Client.GetAsync($"{ErrorProbe}unhandled", Cancellation);
        await response.ReadProblemAsync(HttpStatusCode.InternalServerError);

        RecordingLoggerProvider.LogEntry entry = Assert.Single(
            Api.Logs.Entries.Skip(before),
            IsOutcomeEvent);
        Assert.Equal(LogLevel.Error, entry.Level);
    }

    private static bool IsOutcomeEvent(RecordingLoggerProvider.LogEntry entry) =>
        entry.Category == typeof(MoniPayExceptionHandler).FullName;

    private static string Route(RecordingLoggerProvider.LogEntry entry) =>
        entry.State.First(pair => pair.Key == "Route").Value as string
        ?? throw new Xunit.Sdk.XunitException("The outcome event carries no Route property.");

    private static IReadOnlyList<(string Detail, string Pointer)> Errors(ProblemDetails problem)
    {
        JsonElement errors = Assert.IsType<JsonElement>(problem.Extensions["errors"]);

        return
        [
            .. errors.EnumerateArray().Select(error => (
                error.GetProperty("detail").GetString() ?? string.Empty,
                error.GetProperty("pointer").GetString() ?? string.Empty)),
        ];
    }
}
