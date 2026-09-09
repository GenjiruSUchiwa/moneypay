using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using MoniPay.Kernel;
using MoniPay.Wallet.Contracts;
using MoniPay.Wallet.Domain;

namespace MoniPay.Wallet.Endpoints;

public static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder wallet = app.MapGroup(WalletRoutes.Group).WithTags(WalletTags.Wallet);

        wallet.MapGet(WalletRoutes.Root, GetWallet)
            .WithName(WalletEndpointNames.GetWallet)
            .WithSummary(WalletSummaries.GetWallet)
            .Produces<WalletResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static IResult GetWallet(
        string? currency,
        [FromServices] IStringLocalizer<WalletMessages> messages)
    {
        string requested = currency ?? nameof(Currency.Xaf);

        if (!string.Equals(requested, nameof(Currency.Xaf), StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                title: messages[WalletMessageKeys.UnsupportedCurrency],
                statusCode: StatusCodes.Status400BadRequest);
        }

        WalletBalance balance = WalletBalance.Empty;

        return Results.Ok(new WalletResponse(
            Currency: balance.Balance.Currency.ToString().ToUpperInvariant(),
            Balance: balance.Balance.MinorUnits,
            Held: balance.Held.MinorUnits,
            Available: balance.Available.MinorUnits));
    }
}
