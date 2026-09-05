namespace MoniPay.Sessions.Domain;

/// <summary>The state of one sign-up. Stored as a string so a reordered enum cannot reinterpret rows.</summary>
internal enum SignUpStatus
{
    /// <summary>The code was delivered and has not been checked yet.</summary>
    CodePending,

    /// <summary>The phone is proven; the registration token authorizes completion.</summary>
    PhoneVerified,

    /// <summary>The user and the bootstrap session exist.</summary>
    Completed,

    /// <summary>The verification-attempt limit was reached.</summary>
    Locked,

    /// <summary>The sign-up lifetime has ended; the client must start over.</summary>
    Expired,
}
