using MoniPay.Kernel;

namespace MoniPay.Wallet.Domain;

public sealed record WalletBalance(Money Balance, Money Held)
{
    public Money Available => Balance - Held;

    public static WalletBalance Empty { get; } = new(Money.Xaf(0), Money.Xaf(0));
}
