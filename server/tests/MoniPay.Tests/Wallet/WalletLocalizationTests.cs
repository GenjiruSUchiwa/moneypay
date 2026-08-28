using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MoniPay.Api;
using MoniPay.Kernel;
using MoniPay.Wallet.Endpoints;
using Xunit;

namespace MoniPay.Tests.Wallet;

/// <summary>
/// Drives the real host over HTTP: request localization is middleware, so nothing below the
/// pipeline can prove it works.
/// </summary>
public class WalletLocalizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>A currency MoniPay does not hold, which is what makes the wallet refuse.</summary>
    private static readonly string UnsupportedCurrency = nameof(Currency.Usd).ToUpperInvariant();

    private static readonly Uri Wallet = new(WalletRoutes.Group, UriKind.Relative);

    private static readonly Uri WalletInUnsupportedCurrency =
        new($"{WalletRoutes.Group}?{WalletRoutes.CurrencyQuery}={UnsupportedCurrency}", UriKind.Relative);

    private readonly WebApplicationFactory<Program> factory;

    public WalletLocalizationTests(WebApplicationFactory<Program> factory) => this.factory = factory;

    [Fact]
    public async Task A_refusal_is_written_in_French_when_the_client_asks_for_French()
    {
        var response = await Refusal(acceptLanguage: Localization.French);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Seul le portefeuille en FCFA est disponible.", await TitleOf(response));
    }

    [Fact]
    public async Task The_neutral_resource_answers_an_English_client()
    {
        var response = await Refusal(acceptLanguage: Localization.English);

        Assert.Equal("Only the XAF wallet is available.", await TitleOf(response));
    }

    [Fact]
    public async Task French_is_the_default_when_the_client_states_no_preference()
    {
        // The product's users read French, so a request with no Accept-Language gets French.
        var response = await Refusal(acceptLanguage: null);

        Assert.Equal("Seul le portefeuille en FCFA est disponible.", await TitleOf(response));
    }

    [Fact]
    public async Task A_regional_culture_falls_back_to_its_parent_resource()
    {
        // fr-CM is supported and has no .resx of its own: it must resolve to WalletMessages.fr.
        var response = await Refusal(acceptLanguage: Localization.FrenchCameroon);

        Assert.Equal("Seul le portefeuille en FCFA est disponible.", await TitleOf(response));
    }

    [Fact]
    public async Task The_success_path_carries_no_localized_text()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(Localization.French);

        var response = await client.GetAsync(Wallet, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.Equal(nameof(Currency.Xaf).ToUpperInvariant(), body.GetProperty("currency").GetString());
    }

    private async Task<HttpResponseMessage> Refusal(string? acceptLanguage)
    {
        var client = factory.CreateClient();

        if (acceptLanguage is not null)
        {
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);
        }

        return await client.GetAsync(WalletInUnsupportedCurrency, TestContext.Current.CancellationToken);
    }

    private static async Task<string?> TitleOf(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        return problem.GetProperty("title").GetString();
    }
}
