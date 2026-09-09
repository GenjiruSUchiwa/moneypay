namespace MoniPay.Sessions.Domain;

internal enum SignUpStatus
{
    CodePending,

    PhoneVerified,

    Completed,

    Locked,

    Expired,
}
