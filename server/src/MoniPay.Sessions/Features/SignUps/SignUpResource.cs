using System.Text.Json.Serialization;
using MoniPay.Kernel;
using MoniPay.Kernel.Http;
using MoniPay.Sessions.Domain;
using MoniPay.Sessions.Features.SignUps.Get;
using MoniPay.Sessions.Features.SignUps.Start;
using MoniPay.Sessions.Providers;

namespace MoniPay.Sessions.Features.SignUps;

/// <summary>The documented <c>status</c> values of the <c>signups</c> resource.</summary>
internal static class SignUpStatuses
{
    public const string CodePending = "codePending";

    public const string PhoneVerified = "phoneVerified";

    public const string Completed = "completed";

    public const string Locked = "locked";

    public const string Expired = "expired";

    public static string From(SignUpStatus status) => status switch
    {
        SignUpStatus.CodePending => CodePending,
        SignUpStatus.PhoneVerified => PhoneVerified,
        SignUpStatus.Completed => Completed,
        SignUpStatus.Locked => Locked,
        SignUpStatus.Expired => Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}

/// <summary>The documented <c>codeDelivery</c> values of the <c>signups</c> resource.</summary>
internal static class CodeDeliveryValues
{
    public const string Queued = "queued";

    public const string Sent = "sent";

    public const string Failed = "failed";

    public const string Expired = "expired";

    public static string From(CodeDeliveryState delivery) => delivery switch
    {
        CodeDeliveryState.Queued => Queued,
        CodeDeliveryState.Sent => Sent,
        CodeDeliveryState.Failed => Failed,
        CodeDeliveryState.Expired => Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(delivery), delivery, null),
    };
}

/// <summary>
/// The attributes of the <c>signups</c> resource. One record serves the start and the read: optional
/// members are omitted when null, so a creation response carries its one-time token while a read carries none.
/// </summary>
internal sealed record SignUpAttributes
{
    public required string Status { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CodeDelivery { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SignUpToken { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegistrationToken { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CodeExpiresAt { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CanResendAt { get; init; }

    public required DateTimeOffset SignUpExpiresAt { get; init; }
}

/// <summary>The <c>signups</c> resource. The identifier lives in <c>id</c> only, never in the attributes.</summary>
internal sealed record SignUpResource
{
    public required string Type { get; init; }

    public required string Id { get; init; }

    public required SignUpAttributes Attributes { get; init; }

    public JsonApiLinks? Links { get; init; }
}

/// <summary>Projects handler results onto the <c>signups</c> resource and its links.</summary>
internal static class SignUpResources
{
    public static string Self(SignUpId signUpId) =>
        FormattableString.Invariant($"{SignUpRoutes.Group}/{signUpId.Value}");

    public static SignUpResource FromStart(StartSignUpResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new SignUpResource
        {
            Type = SignUpResourceTypes.SignUps,
            Id = result.SignUpId.Value.ToString(),
            Attributes = new SignUpAttributes
            {
                Status = SignUpStatuses.CodePending,
                CodeDelivery = CodeDeliveryValues.Queued,
                SignUpToken = result.SignUpToken,
                CodeExpiresAt = result.CodeExpiresAt,
                CanResendAt = result.CanResendAt,
                SignUpExpiresAt = result.SignUpExpiresAt,
            },
            Links = new JsonApiLinks { Self = Self(result.SignUpId) },
        };
    }

    public static SignUpResource FromView(SignUpId signUpId, SignUpView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return new SignUpResource
        {
            Type = SignUpResourceTypes.SignUps,
            Id = signUpId.Value.ToString(),
            Attributes = new SignUpAttributes
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
