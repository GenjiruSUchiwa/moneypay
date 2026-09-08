namespace MoniPay.Users;

/// <summary>
/// The resource keys in <c>Resources/UserMessages.resx</c>. A localizer returns the key itself
/// when it finds no entry, so a typo in a literal is a silent English-looking string shipped to
/// a French user. Naming them here makes that a compile error instead.
/// </summary>
internal static class UserMessageKeys
{
    public const string WelcomeEmailSubject = nameof(WelcomeEmailSubject);

    public const string WelcomeEmailBody = nameof(WelcomeEmailBody);
}
