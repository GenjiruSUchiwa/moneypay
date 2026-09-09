namespace MoniPay.Sessions.Features.Sessions;

/// <summary>The operation names. They become the <c>operationId</c> in the OpenAPI document, so renaming one is a breaking client change.</summary>
internal static class SessionEndpointNames
{
    public const string CreateSessionRefresh = nameof(CreateSessionRefresh);

    public const string GetCurrentSession = nameof(GetCurrentSession);

    public const string DeleteCurrentSession = nameof(DeleteCurrentSession);
}
