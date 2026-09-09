namespace MoniPay.Sessions.Features.SignUps;

internal static class SignUpRoutes
{
    public const string Group = "/signups";

    public const string Start = "";

    public const string ById = "/{signUpId:guid}";

    public const string VerificationCodeDeliveries = "/{signUpId:guid}/verification-code-deliveries";

    public const string PhoneVerifications = "/{signUpId:guid}/phone-verifications";

    public const string Completions = "/{signUpId:guid}/completions";
}
