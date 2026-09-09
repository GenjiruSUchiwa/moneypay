namespace MoniPay.Kernel.Http;

/// <summary>
/// Endpoint metadata marking a JSON:API route that accepts no request body: the command is fully
/// named by its route and its credential. The host refuses a body it detects on such a route
/// before it reads it, so a body can neither be parsed nor smuggled past the check.
/// </summary>
public sealed record JsonApiNoBody;
