namespace MoniPay.Api.Errors;

/// <summary>
/// One item of a validation problem's <c>errors</c> array. The wire shape is exactly
/// <c>detail</c> and <c>pointer</c>; the code that produced the failure stays server-side.
/// </summary>
internal sealed record ValidationProblemItem(string Detail, string Pointer);
