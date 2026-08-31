using MoniPay.Kernel;

namespace MoniPay.Users.Domain;

/// <summary>
/// A registered user. The entity protects nothing itself: the caller hands it values that are
/// already normalized and already encrypted or hashed, so the encryption key never reaches the
/// domain. A user exists only with its two legal consents — registration records them.
/// </summary>
internal sealed class User
{
    private readonly List<UserConsent> consents = [];

    private User()
    {
    }

    public UserId Id { get; private set; }

    /// <summary>The sign-up this user was provisioned from — the idempotency key of registration.</summary>
    public SignUpId SignUpId { get; private set; }

    public Ciphertext FirstName { get; private set; }

    public Ciphertext LastName { get; private set; }

    public ProtectedContact Phone => new(PhoneCiphertext, PhoneHash);

    public ProtectedContact Email => new(EmailCiphertext, EmailHash);

    public Locale Locale { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<UserConsent> Consents => consents;

    private Ciphertext PhoneCiphertext { get; set; }

    private LookupHash PhoneHash { get; set; }

    private Ciphertext EmailCiphertext { get; set; }

    private LookupHash EmailHash { get; set; }

    /// <summary>
    /// Registers a user who accepted the terms and the privacy policy at the versions the
    /// client displayed. <paramref name="registeredAt"/> is the server time, stamped on the
    /// user and on both consents.
    /// </summary>
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
            PhoneCiphertext = phone.Ciphertext,
            PhoneHash = phone.Hash,
            EmailCiphertext = email.Ciphertext,
            EmailHash = email.Hash,
            Locale = locale,
            CreatedAt = registeredAt,
        };
        user.consents.Add(new UserConsent(id, LegalDocumentKind.Terms, termsVersion, registeredAt));
        user.consents.Add(new UserConsent(id, LegalDocumentKind.Privacy, privacyVersion, registeredAt));

        return user;
    }
}
