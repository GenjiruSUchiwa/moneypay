namespace MoniPay.Kernel;

/// <summary>
/// Route URIs shared across modules. Each entry is owned by the module that serves it — the
/// constant only lets other modules link to it without a sibling reference, which the
/// architecture forbids. Each route is declared here once and mapped from here, so there is no
/// second copy to keep in step.
/// </summary>
public static class MoniPayRoutes
{
    /// <summary>
    /// The user of the calling credential. Served by the Users module, which maps this very
    /// constant; Sessions names it as the <c>related</c> link of a session's <c>user</c>
    /// relationship.
    /// </summary>
    public const string CurrentUser = "/users/me";
}
