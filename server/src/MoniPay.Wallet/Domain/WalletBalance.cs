using MoniPay.Kernel;

namespace MoniPay.Wallet.Domain;

/// <summary>
/// What the wallet can commit right now. <see cref="Available"/> is the balance minus the holds
/// an accepted authorization has placed, and it — never <see cref="Balance"/> — is what an
/// authorization compares a requested amount against.
/// </summary>
public sealed record WalletBalance(Money Balance, Money Held)
{
    public Money Available => Balance - Held;

    public static WalletBalance Empty { get; } = new(Money.Xaf(0), Money.Xaf(0));
}
