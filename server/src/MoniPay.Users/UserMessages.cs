namespace MoniPay.Users;

/// <summary>
/// The lookup key for this module's translated strings. It carries no members: an
/// <c>IStringLocalizer&lt;UserMessages&gt;</c> resolves
/// <c>Resources/UserMessages[.&lt;culture&gt;].resx</c> from this type's name and namespace,
/// so the module owns its copy the same way it owns its domain. Internal, unlike the Sessions
/// marker, so the Users public surface stays the two host-composed port types.
/// </summary>
internal sealed class UserMessages;
