namespace MoniPay.Sessions.Features.SignUps;

public static class SignUpRateLimitPolicies
{
    public const string Start = "sign-up-start";

    public const string Resend = "sign-up-resend";

    public const string Verify = "sign-up-verify";

    public const string Complete = "sign-up-complete";
}
