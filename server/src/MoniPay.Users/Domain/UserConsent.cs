using MoniPay.Kernel;

namespace MoniPay.Users.Domain;

/// <summary>
/// One accepted legal document, keyed by user and kind: a user accepts each document once, and
/// the version shown by the client is kept with the server receipt time.
/// </summary>
internal sealed class UserConsent
{
    private UserConsent(UserId userId, LegalDocumentKind documentKind, string documentVersion, DateTimeOffset acceptedAt)
    {
        UserId = userId;
        DocumentKind = documentKind;
        DocumentVersion = documentVersion;
        AcceptedAt = acceptedAt;
    }

    public UserId UserId { get; private set; }

    public LegalDocumentKind DocumentKind { get; private set; }

    public string DocumentVersion { get; private set; }

    public DateTimeOffset AcceptedAt { get; private set; }

    /// <summary>Records an acceptance. <paramref name="acceptedAt"/> is the server receipt time.</summary>
    public static UserConsent Accept(
        UserId userId,
        LegalDocumentKind documentKind,
        string documentVersion,
        DateTimeOffset acceptedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentVersion);
        return new UserConsent(userId, documentKind, documentVersion, acceptedAt);
    }
}
