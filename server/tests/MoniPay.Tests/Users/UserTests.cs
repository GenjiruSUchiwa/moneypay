using MoniPay.Kernel;
using MoniPay.Users.Domain;
using Xunit;

namespace MoniPay.Tests.Users;

public sealed class UserTests
{
    private static readonly DateTimeOffset RegisteredAt = new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Registering_a_user_records_both_legal_consents_at_the_server_time()
    {
        UserId id = UserId.New();

        User user = Register(id, termsVersion: "terms-2026-08", privacyVersion: "privacy-2026-07");

        Assert.Collection(
            user.Consents.OrderBy(consent => consent.DocumentKind),
            terms =>
            {
                Assert.Equal(id, terms.UserId);
                Assert.Equal(LegalDocumentKind.Terms, terms.DocumentKind);
                Assert.Equal("terms-2026-08", terms.DocumentVersion);
                Assert.Equal(RegisteredAt, terms.AcceptedAt);
            },
            privacy =>
            {
                Assert.Equal(LegalDocumentKind.Privacy, privacy.DocumentKind);
                Assert.Equal("privacy-2026-07", privacy.DocumentVersion);
                Assert.Equal(RegisteredAt, privacy.AcceptedAt);
            });
    }

    [Theory]
    [InlineData("", "privacy-1")]
    [InlineData("terms-1", " ")]
    public void Registering_without_a_document_version_is_refused(string termsVersion, string privacyVersion)
    {
        Assert.Throws<ArgumentException>(() => Register(UserId.New(), termsVersion, privacyVersion));
    }

    private static User Register(UserId id, string termsVersion, string privacyVersion) =>
        User.Register(
            id,
            SignUpId.New(),
            new Ciphertext("first"),
            new Ciphertext("last"),
            new ProtectedContact(new Ciphertext("phone"), new LookupHash([1])),
            new ProtectedContact(new Ciphertext("email"), new LookupHash([2])),
            Locale.FrenchCameroon,
            termsVersion,
            privacyVersion,
            RegisteredAt);
}
