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

internal sealed record StartSignUpResourceAttributes
{
    public required SignUpStatusValue Status { get; init; }

    public required CodeDeliveryValue CodeDelivery { get; init; }

    public required string SignUpToken { get; init; }

    public required DateTimeOffset CodeExpiresAt { get; init; }

    public required DateTimeOffset CanResendAt { get; init; }

    public required DateTimeOffset SignUpExpiresAt { get; init; }
}

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

internal sealed record VerifiedSignUpResourceAttributes
{
    public required SignUpStatusValue Status { get; init; }

    public required string RegistrationToken { get; init; }

    public required DateTimeOffset SignUpExpiresAt { get; init; }
}

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
