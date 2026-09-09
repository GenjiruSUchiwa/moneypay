using System.Text.Json.Serialization;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps.Get;
using MoniPay.Sessions.Features.SignUps.ResendCode;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Features.SignUps.VerifyPhone;
using MoniPay.Sessions.Providers;

namespace MoniPay.Sessions.Features.SignUps;

/// <summary>
/// The documented <c>status</c> values of the <c>signups</c> resource. The member names are the
/// wire strings; the converter on this enum is the single definition of them.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SignUpStatusValue>))]
internal enum SignUpStatusValue
{
    [JsonStringEnumMemberName("codePending")]
    CodePending,

    [JsonStringEnumMemberName("phoneVerified")]
    PhoneVerified,

    [JsonStringEnumMemberName("completed")]
    Completed,

    [JsonStringEnumMemberName("locked")]
    Locked,

    [JsonStringEnumMemberName("expired")]
    Expired,
}

/// <summary>Maps the domain states onto the wire values, so the two can evolve independently.</summary>
internal static class SignUpStatuses
{
    public static SignUpStatusValue From(SignUpStatus status) => status switch
    {
        SignUpStatus.CodePending => SignUpStatusValue.CodePending,
        SignUpStatus.PhoneVerified => SignUpStatusValue.PhoneVerified,
        SignUpStatus.Completed => SignUpStatusValue.Completed,
        SignUpStatus.Locked => SignUpStatusValue.Locked,
        SignUpStatus.Expired => SignUpStatusValue.Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}

/// <summary>The documented <c>codeDelivery</c> values of the <c>signups</c> resource.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CodeDeliveryValue>))]
internal enum CodeDeliveryValue
{
    [JsonStringEnumMemberName("queued")]
    Queued,

    [JsonStringEnumMemberName("sent")]
    Sent,

    [JsonStringEnumMemberName("failed")]
    Failed,

    [JsonStringEnumMemberName("expired")]
    Expired,
}

/// <summary>Maps the domain delivery states onto the wire values.</summary>
internal static class CodeDeliveryValues
{
    public static CodeDeliveryValue From(CodeDeliveryState delivery) => delivery switch
    {
        CodeDeliveryState.Queued => CodeDeliveryValue.Queued,
        CodeDeliveryState.Sent => CodeDeliveryValue.Sent,
        CodeDeliveryState.Failed => CodeDeliveryValue.Failed,
        CodeDeliveryState.Expired => CodeDeliveryValue.Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(delivery), delivery, null),
    };
}

/// <summary>
/// The attributes returned by the start operation. Every member is present because the operation
/// has created the sign-up and queued its first code.
/// </summary>
internal sealed record StartSignUpResourceAttributes
{
    public required SignUpStatusValue Status { get; init; }

    public required CodeDeliveryValue CodeDelivery { get; init; }

    public required string SignUpToken { get; init; }

    public required DateTimeOffset CodeExpiresAt { get; init; }

    public required DateTimeOffset CanResendAt { get; init; }

    public required DateTimeOffset SignUpExpiresAt { get; init; }
}

/// <summary>
/// The attributes returned by the read operation. A delivery or code expiry can be absent after
/// the workflow moves past the code stage, so those null members remain omitted on the wire.
/// </summary>
internal sealed record ReadSignUpResourceAttributes
{
    public required SignUpStatusValue Status { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CodeDeliveryValue? CodeDelivery { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CodeExpiresAt { get; init; }

    public required DateTimeOffset CanResendAt { get; init; }

    public required DateTimeOffset SignUpExpiresAt { get; init; }
}

/// <summary>
/// The attributes returned by the phone-verification operation: the one moment the registration
/// credential exists. The sign-up token is void from here on, and the credential is never read
/// back.
/// </summary>
internal sealed record VerifiedSignUpResourceAttributes
{
    public required SignUpStatusValue Status { get; init; }

    public required string RegistrationToken { get; init; }

    public required DateTimeOffset SignUpExpiresAt { get; init; }
}

/// <summary>Projects handler results onto the <c>signups</c> resource and its links.</summary>
internal static class SignUpResources
{
    public static string Self(SignUpId signUpId) =>
        FormattableString.Invariant($"{SignUpRoutes.Group}/{signUpId.Value}");

    public static JsonApiResponseResource<StartSignUpResourceAttributes> FromStart(StartSignUpResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new JsonApiResponseResource<StartSignUpResourceAttributes>
        {
            Type = SignUpResourceTypes.SignUps,
            Id = result.SignUpId.Value.ToString(),
            Attributes = new StartSignUpResourceAttributes
            {
                Status = SignUpStatusValue.CodePending,
                CodeDelivery = CodeDeliveryValue.Queued,
                SignUpToken = result.SignUpToken,
                CodeExpiresAt = result.CodeExpiresAt,
                CanResendAt = result.CanResendAt,
                SignUpExpiresAt = result.SignUpExpiresAt,
            },
            Links = new JsonApiLinks { Self = Self(result.SignUpId) },
        };
    }

    /// <summary>
    /// The read form after a resend: the code timing it refreshed. The aggregate only rotates a
    /// code while the workflow is pending one, so the state and the queued delivery are known here.
    /// </summary>
    public static JsonApiResponseResource<ReadSignUpResourceAttributes> FromResend(
        SignUpId signUpId,
        CreateVerificationCodeDeliveryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new JsonApiResponseResource<ReadSignUpResourceAttributes>
        {
            Type = SignUpResourceTypes.SignUps,
            Id = signUpId.Value.ToString(),
            Attributes = new ReadSignUpResourceAttributes
            {
                Status = SignUpStatusValue.CodePending,
                CodeDelivery = CodeDeliveryValue.Queued,
                CodeExpiresAt = result.CodeExpiresAt,
                CanResendAt = result.CanResendAt,
                SignUpExpiresAt = result.SignUpExpiresAt,
            },
            Links = new JsonApiLinks { Self = Self(signUpId) },
        };
    }

    public static JsonApiResponseResource<ReadSignUpResourceAttributes> FromView(SignUpId signUpId, SignUpView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return new JsonApiResponseResource<ReadSignUpResourceAttributes>
        {
            Type = SignUpResourceTypes.SignUps,
            Id = signUpId.Value.ToString(),
            Attributes = new ReadSignUpResourceAttributes
            {
                Status = SignUpStatuses.From(view.Status),
                CodeDelivery = view.CodeDelivery is { } delivery ? CodeDeliveryValues.From(delivery) : null,
                CodeExpiresAt = view.CodeExpiresAt,
                CanResendAt = view.CanResendAt,
                SignUpExpiresAt = view.SignUpExpiresAt,
            },
            Links = new JsonApiLinks { Self = Self(signUpId) },
        };
    }

    public static JsonApiResponseResource<VerifiedSignUpResourceAttributes> FromVerification(
        CreatePhoneVerificationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new JsonApiResponseResource<VerifiedSignUpResourceAttributes>
        {
            Type = SignUpResourceTypes.SignUps,
            Id = result.SignUpId.Value.ToString(),
            Attributes = new VerifiedSignUpResourceAttributes
            {
                Status = SignUpStatusValue.PhoneVerified,
                RegistrationToken = result.RegistrationToken,
                SignUpExpiresAt = result.SignUpExpiresAt,
            },
            Links = new JsonApiLinks { Self = Self(result.SignUpId) },
        };
    }
}
