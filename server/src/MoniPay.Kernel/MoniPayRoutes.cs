namespace MoniPay.Kernel;

/// <summary>
/// Route URIs shared across modules. Each entry is owned by the module that serves it — the
/// constant only lets other modules link to it without a sibling reference, which the
/// architecture forbids. A rename must update the constant and its owner together; the Users
/// tests pin that equality.
/// </summary>
public static class MoniPayRoutes
{
    /// <summary>
    /// The user of the calling credential. Owned by the Users module
    /// (<c>UserRoutes.Group + UserRoutes.Me</c>); Sessions names it as the <c>related</c> link of
    /// a session's <c>user</c> relationship.
    /// </summary>
    public const string CurrentUser = "/users/me";
}
