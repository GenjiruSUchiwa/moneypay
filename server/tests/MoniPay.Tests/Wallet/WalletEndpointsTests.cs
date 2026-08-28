using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MoniPay.Wallet.Endpoints;
using Xunit;

namespace MoniPay.Tests.Wallet;

public class WalletEndpointsTests
{
    [Fact]
    public void The_wallet_module_maps_its_own_route_without_the_host()
    {
        // The point of the layout: a module's HTTP surface composes on its own, so a test can
        // map one module without booting the API host.
        IEndpointRouteBuilder app = WebApplication.CreateBuilder().Build();

        app.MapWalletEndpoints();

        // Read the builder's own data sources: the DI-registered EndpointDataSource is only
        // populated once the host builds its request pipeline, which this test never does.
        var routes = app.DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => NormalizeGroupRoot(endpoint.RoutePattern.RawText));

        Assert.Contains(WalletRoutes.Group, routes);
    }

    [Fact]
    public void The_route_is_named_so_the_generated_client_gets_a_stable_method()
    {
        IEndpointRouteBuilder app = WebApplication.CreateBuilder().Build();

        app.MapWalletEndpoints();

        var names = app.DataSources
            .SelectMany(source => source.Endpoints)
            .Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);

        Assert.Contains(WalletEndpointNames.GetWallet, names);
    }

    /// <summary>
    /// A group's own root renders as "/wallet/" in the raw pattern, while the route that is
    /// actually served — and the path in the generated OpenAPI document — is "/wallet". The
    /// trailing slash is a rendering artefact of MapGroup, so it is normalized away rather than
    /// baked into the constant.
    /// </summary>
    private static string? NormalizeGroupRoot(string? rawText) =>
        rawText is { Length: > 1 } ? rawText.TrimEnd('/') : rawText;
}
