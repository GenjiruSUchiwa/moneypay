namespace MoniPay.Sessions.Domain;

/// <summary>
/// The result of checking a verification code. A mismatch is returned, never thrown: the handler
/// must persist the attempt count before it answers, and an exception would skip that save.
/// </summary>
internal enum PhoneVerificationOutcome
{
    /// <summary>The code matched; the phone is proven.</summary>
    Verified,

    /// <summary>The code did not match; an attempt was consumed.</summary>
    Mismatch,

    /// <summary>The last allowed attempt failed; the sign-up locked.</summary>
    Locked,
}
