namespace MoniPay.Sessions.Features.SignUps;

internal static class SignUpSummaries
{
    public const string StartSignUp = "Starts a sign-up and queues its verification code.";

    public const string GetSignUp = "Returns the state of a sign-up.";

    public const string CreateVerificationCodeDelivery = "Replaces the verification code and queues its delivery.";

    public const string CreatePhoneVerification = "Checks the verification code and issues the registration credential.";

    public const string CreateSignUpCompletion = "Creates the user and opens the first session.";
}
