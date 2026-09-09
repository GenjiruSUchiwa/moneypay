using System.Text.Json.Serialization;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps.Get;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Providers;

namespace MoniPay.Sessions.Features.SignUps;

/// <summary>The documented <c>status</c> values of the <c>signups</c> resource.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SignUpStatusValue>))]
internal enum SignUpStatusValue
{
    [JsonStringEnumMemberName(SignUpStatuses.CodePending)]
    CodePending,

    [JsonStringEnumMemberName(SignUpStatuses.PhoneVerified)]
    PhoneVerified,

    [JsonStringEnumMemberName(SignUpStatuses.Completed)]
    Completed,

    [JsonStringEnumMemberName(SignUpStatuses.Locked)]
    Locked,

    [JsonStringEnumMemberName(SignUpStatuses.Expired)]
    Expired,
}

/// <summary>The documented <c>status</c> values of the <c>signups</c> resource.</summary>
internal static class SignUpStatuses
{
    public const string CodePending = "codePending";

    public const string PhoneVerified = "phoneVerified";

    public const string Completed = "completed";

    public const string Locked = "locked";

    public const string Expired = "expired";

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
    [JsonStringEnumMemberName(CodeDeliveryValues.Queued)]
    Queued,

    [JsonStringEnumMemberName(CodeDeliveryValues.Sent)]
    Sent,

    [JsonStringEnumMemberName(CodeDeliveryValues.Failed)]
    Failed,

    [JsonStringEnumMemberName(CodeDeliveryValues.Expired)]
    Expired,
}

/// <summary>The documented <c>codeDelivery</c> values of the <c>signups</c> resource.</summary>
internal static class CodeDeliveryValues
{
    public const string Queued = "queued";

    public const string Sent = "sent";

    public const string Failed = "failed";

    public const string Expired = "expired";

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

/// <summary>The <c>signups</c> resource. The identifier lives in <c>id</c> only, never in the attributes.</summary>
internal sealed record SignUpResource<TAttributes> where TAttributes : notnull
{
    public required string Type { get; init; }

    public required string Id { get; init; }

    public required TAttributes Attributes { get; init; }

    public JsonApiLinks? Links { get; init; }
}

/// <summary>Projects handler results onto the <c>signups</c> resource and its links.</summary>
internal static class SignUpResources
{
    public static string Self(SignUpId signUpId) =>
        FormattableString.Invariant($"{SignUpRoutes.Group}/{signUpId.Value}");

    public static SignUpResource<StartSignUpResourceAttributes> FromStart(StartSignUpResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new SignUpResource<StartSignUpResourceAttributes>
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

    public static SignUpResource<ReadSignUpResourceAttributes> FromView(SignUpId signUpId, SignUpView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return new SignUpResource<ReadSignUpResourceAttributes>
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
}
