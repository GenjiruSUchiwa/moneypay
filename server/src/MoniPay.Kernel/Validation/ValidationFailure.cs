namespace MoniPay.Kernel.Validation;

/// <summary>Describes one invalid member in a request document.</summary>
public sealed record ValidationFailure(string Pointer, string Code);
