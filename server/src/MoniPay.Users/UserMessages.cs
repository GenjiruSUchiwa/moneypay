namespace MoniPay.Users;

/// <summary>
/// The lookup key for this module's translated strings. It carries no members: an
/// <c>IStringLocalizer&lt;UserMessages&gt;</c> resolves
/// <c>Resources/UserMessages[.&lt;culture&gt;].resx</c> from this type's name and namespace,
/// so the module owns its copy the same way it owns its domain. Public like the Sessions marker
/// so the host can resolve this module's problem text without reaching into the module.
/// </summary>
public sealed class UserMessages;
