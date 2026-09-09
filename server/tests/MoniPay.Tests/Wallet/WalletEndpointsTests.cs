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
        IEndpointRouteBuilder app = WebApplication.CreateBuilder().Build();

        app.MapWalletEndpoints();

        IEnumerable<string?> routes = app.DataSources
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
        IEnumerable<string?> names = app.DataSources
            .SelectMany(source => source.Endpoints)
            .Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);

        Assert.Contains(WalletEndpointNames.GetWallet, names);
    }

    private static string? NormalizeGroupRoot(string? rawText) =>
        rawText is { Length: > 1 } ? rawText.TrimEnd('/') : rawText;
}
