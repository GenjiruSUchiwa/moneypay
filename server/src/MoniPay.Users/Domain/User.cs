using MoniPay.Kernel;

namespace MoniPay.Users.Domain;

/// <summary>
/// A registered user. Every contact value is stored twice: encrypted so it can be read back,
/// and as a keyed hash so it can be looked up without decrypting the whole table. The entity
/// never protects anything itself — the caller passes values that are already normalized and
/// already encrypted or hashed, so the encryption key never reaches the domain.
/// </summary>
internal sealed class User
{
    private User(
        UserId id,
        Guid signUpId,
        string firstNameCiphertext,
        string lastNameCiphertext,
        string phoneCiphertext,
        byte[] phoneLookupHash,
        string emailCiphertext,
        byte[] emailLookupHash,
        string locale,
        DateTimeOffset createdAt)
    {
        Id = id;
        SignUpId = signUpId;
        FirstNameCiphertext = firstNameCiphertext;
        LastNameCiphertext = lastNameCiphertext;
        PhoneCiphertext = phoneCiphertext;
        PhoneLookupHash = phoneLookupHash;
        EmailCiphertext = emailCiphertext;
        EmailLookupHash = emailLookupHash;
        Locale = locale;
        CreatedAt = createdAt;
    }

    public UserId Id { get; private set; }

    /// <summary>The sign-up this user was provisioned from — the idempotency key of registration.</summary>
    public Guid SignUpId { get; private set; }

    public string FirstNameCiphertext { get; private set; }

    public string LastNameCiphertext { get; private set; }

    public string PhoneCiphertext { get; private set; }

    public byte[] PhoneLookupHash { get; private set; }

    public string EmailCiphertext { get; private set; }

    public byte[] EmailLookupHash { get; private set; }

    /// <summary>A supported BCP-47 tag; the client formats against it.</summary>
    public string Locale { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a user from values the caller has already normalized and protected.</summary>
    public static User Register(
        UserId id,
        Guid signUpId,
        string firstNameCiphertext,
        string lastNameCiphertext,
        string phoneCiphertext,
        byte[] phoneLookupHash,
        string emailCiphertext,
        byte[] emailLookupHash,
        string locale,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstNameCiphertext);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastNameCiphertext);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneCiphertext);
        ArgumentException.ThrowIfNullOrWhiteSpace(emailCiphertext);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        ArgumentNullException.ThrowIfNull(phoneLookupHash);
        ArgumentNullException.ThrowIfNull(emailLookupHash);

        return new User(
            id,
            signUpId,
            firstNameCiphertext,
            lastNameCiphertext,
            phoneCiphertext,
            phoneLookupHash,
            emailCiphertext,
            emailLookupHash,
            locale,
            createdAt);
    }
}
