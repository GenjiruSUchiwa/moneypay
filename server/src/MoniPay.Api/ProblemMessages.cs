namespace MoniPay.Api;

/// <summary>
/// The lookup key for the host's translated problem text. Like a module's message marker, it
/// carries no members: an <c>IStringLocalizer&lt;ProblemMessages&gt;</c> resolves
/// <c>Resources/ProblemMessages[.&lt;culture&gt;].resx</c> from this type's name and namespace.
/// </summary>
public sealed class ProblemMessages;
