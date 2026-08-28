namespace MoniPay.Wallet.Contracts;

/// <summary>
/// What <c>GET /wallet</c> answers. Amounts are XAF minor units, which for a currency with no
/// minor unit means whole francs. The contract carries the currency so a client never has to
/// infer the scale from the endpoint it called.
/// </summary>
/// <param name="Currency">ISO 4217 alphabetic code, always <c>XAF</c> today.</param>
/// <param name="Balance">Everything the wallet holds, in minor units.</param>
/// <param name="Held">The part reserved by authorizations awaiting settlement.</param>
/// <param name="Available">Balance minus held: what a new authorization may draw on.</param>
public sealed record WalletResponse(string Currency, long Balance, long Held, long Available);
