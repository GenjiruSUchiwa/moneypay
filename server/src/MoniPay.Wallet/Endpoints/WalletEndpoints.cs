using Microsoft.AspNetCore.Builder;
// A class library gets none of the Web SDK's implicit usings, so a module names the routing
// and endpoint-metadata namespaces itself.
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using MoniPay.Kernel;
using MoniPay.Wallet.Contracts;
using MoniPay.Wallet.Domain;

namespace MoniPay.Wallet.Endpoints;

/// <summary>
/// The HTTP surface of the wallet module. It lives here, not in the host, so that adding an
/// endpoint touches one project: the module that owns the data behind it. The host only calls
/// <see cref="MapWalletEndpoints"/>.
/// </summary>
public static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // One group per module: the prefix is declared once and every route below inherits it.
        var wallet = app.MapGroup(WalletRoutes.Group).WithTags(WalletTags.Wallet);

        wallet.MapGet(WalletRoutes.Root, GetWallet)
            .WithName(WalletEndpointNames.GetWallet)
            .WithSummary(WalletSummaries.GetWallet)
            .Produces<WalletResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    /// <param name="currency">ISO 4217 code. MoniPay holds one wallet, in XAF.</param>
    /// <param name="messages">
    /// The module's own translations. A refusal is read by the user, so its text is localized;
    /// the machine-readable part of the response never is.
    /// </param>
    private static IResult GetWallet(
        string? currency,
        // Explicit: without it the binder infers an unregistered service as a request body, so
        // the route only composes when the host happens to have registered localization.
        [FromServices] IStringLocalizer<WalletMessages> messages)
    {
        var requested = currency ?? nameof(Currency.Xaf);

        if (!string.Equals(requested, nameof(Currency.Xaf), StringComparison.OrdinalIgnoreCase))
        {
            // The title is what the user is shown, so it comes from the resx. The status code
            // and the type stay invariant: a client switches on those, never on the prose.
            return Results.Problem(
                title: messages[WalletMessageKeys.UnsupportedCurrency],
                statusCode: StatusCodes.Status400BadRequest);
        }

        // A stub until the ledger lands: the shape of the answer is the contract, and it is
        // worth agreeing on before there is a balance to put in it.
        var balance = WalletBalance.Empty;

        return Results.Ok(new WalletResponse(
            Currency: balance.Balance.Currency.ToString().ToUpperInvariant(),
            Balance: balance.Balance.MinorUnits,
            Held: balance.Held.MinorUnits,
            Available: balance.Available.MinorUnits));
    }
}
