using MoniPay.Kernel;

namespace MoniPay.Users.Domain;

internal sealed class User
{
    private readonly List<UserConsent> consents = [];

    private User()
    {
    }

    public UserId Id { get; private set; }

    public SignUpId SignUpId { get; private set; }

    public Ciphertext FirstName { get; private set; }

    public Ciphertext LastName { get; private set; }

    public required ProtectedContact Phone { get; init; }

    public required ProtectedContact Email { get; init; }

    public Locale Locale { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<UserConsent> Consents => consents;

    public static User Register(
        UserId id,
        SignUpId signUpId,
        Ciphertext firstName,
        Ciphertext lastName,
        ProtectedContact phone,
        ProtectedContact email,
        Locale locale,
        string termsVersion,
        string privacyVersion,
        DateTimeOffset registeredAt)
    {
        User user = new()
        {
            Id = id,
            SignUpId = signUpId,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Email = email,
            Locale = locale,
            CreatedAt = registeredAt,
        };
        user.consents.Add(new UserConsent(id, LegalDocumentKind.Terms, termsVersion, registeredAt));
        user.consents.Add(new UserConsent(id, LegalDocumentKind.Privacy, privacyVersion, registeredAt));

        return user;
    }
}
