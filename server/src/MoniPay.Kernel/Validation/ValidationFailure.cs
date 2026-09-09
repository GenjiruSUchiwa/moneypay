namespace MoniPay.Kernel.Validation;

public sealed record ValidationFailure(string Pointer, string Code);
