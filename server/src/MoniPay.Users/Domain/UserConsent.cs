using MoniPay.Kernel;

namespace MoniPay.Users.Domain;

/// <summary>
/// One accepted legal document, keyed by user and kind. It is created by <see cref="User"/>
/// only, so a consent can never exist without its user, nor a user without its consents.
/// </summary>
internal sealed class UserConsent
{
    internal UserConsent(UserId userId, LegalDocumentKind documentKind, string documentVersion, DateTimeOffset acceptedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentVersion);

        UserId = userId;
        DocumentKind = documentKind;
        DocumentVersion = documentVersion;
        AcceptedAt = acceptedAt;
    }

    public UserId UserId { get; }

    public LegalDocumentKind DocumentKind { get; }

    /// <summary>The version the client displayed, kept as evidence of what was accepted.</summary>
    public string DocumentVersion { get; }

    /// <summary>The server receipt time, never the client's clock.</summary>
    public DateTimeOffset AcceptedAt { get; }
}
