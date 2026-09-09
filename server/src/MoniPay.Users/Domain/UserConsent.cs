using MoniPay.Kernel;

namespace MoniPay.Users.Domain;

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

    public string DocumentVersion { get; }

    public DateTimeOffset AcceptedAt { get; }
}
