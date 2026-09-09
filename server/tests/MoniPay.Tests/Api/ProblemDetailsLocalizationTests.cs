using System.Net;
using Microsoft.AspNetCore.Mvc;
using MoniPay.Kernel;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Api;

public sealed class ProblemDetailsLocalizationTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private const string Refusal = "/test/errors/refusal";

    [Fact]
    public async Task French_is_the_default_when_the_client_states_no_preference()
    {
        ProblemDetails problem = await RefusalAsync(acceptLanguage: null);

        Assert.Equal("Trop de requêtes.", problem.Title);
    }

    [Fact]
    public async Task English_is_served_when_requested()
    {
        ProblemDetails problem = await RefusalAsync(Locale.EnglishTag);

        Assert.Equal("Too many requests.", problem.Title);
    }

    [Fact]
    public async Task A_regional_culture_falls_back_to_its_parent_resource()
    {
        ProblemDetails problem = await RefusalAsync(Locale.FrenchCameroonTag);

        Assert.Equal("Trop de requêtes.", problem.Title);
    }

    [Fact]
    public async Task An_unsupported_locale_uses_the_configured_default()
    {
        ProblemDetails problem = await RefusalAsync("de-DE");

        Assert.Equal("Trop de requêtes.", problem.Title);
    }

    [Fact]
    public async Task Concurrent_requests_do_not_share_culture_state()
    {
        ProblemDetails[] problems = await Task.WhenAll(
            RefusalAsync(Locale.FrenchTag),
            RefusalAsync(Locale.EnglishTag));

        Assert.Equal("Trop de requêtes.", problems[0].Title);
        Assert.Equal("Too many requests.", problems[1].Title);
    }

    private async Task<ProblemDetails> RefusalAsync(string? acceptLanguage)
    {
        using HttpClient client = Api.CreateClient();

        if (acceptLanguage is not null)
        {
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);
        }

        using HttpResponseMessage response = await client.GetAsync(Refusal, Cancellation);

        return await response.ReadProblemAsync(HttpStatusCode.TooManyRequests);
    }
}
