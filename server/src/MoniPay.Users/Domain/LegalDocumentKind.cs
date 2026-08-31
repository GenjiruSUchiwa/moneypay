namespace MoniPay.Users.Domain;

/// <summary>The legal document a user accepted. Persisted by name, never by ordinal.</summary>
internal enum LegalDocumentKind
{
    Terms,
    Privacy,
}
