namespace MoniPay.Sessions.Features.SignUps;

/// <summary>The operation names. They become the <c>operationId</c> in the OpenAPI document, so renaming one is a breaking client change.</summary>
internal static class SignUpEndpointNames
{
    public const string StartSignUp = nameof(StartSignUp);

    public const string GetSignUp = nameof(GetSignUp);

    public const string CreateVerificationCodeDelivery = nameof(CreateVerificationCodeDelivery);

    public const string CreatePhoneVerification = nameof(CreatePhoneVerification);

    public const string CreateSignUpCompletion = nameof(CreateSignUpCompletion);
}
