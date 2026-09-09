namespace MoniPay.Api.Errors;

internal sealed record ValidationProblemItem(string Detail, string Pointer);
