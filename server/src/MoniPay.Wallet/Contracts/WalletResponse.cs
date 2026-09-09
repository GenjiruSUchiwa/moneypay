namespace MoniPay.Wallet.Contracts;

public sealed record WalletResponse(string Currency, long Balance, long Held, long Available);
