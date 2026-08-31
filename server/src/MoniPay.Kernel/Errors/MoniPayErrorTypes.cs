using System.Net;

namespace MoniPay.Kernel.Errors;

/// <summary>Stable problem types shared by the host and every module, each bound to its status.</summary>
public static class MoniPayErrorTypes
{
    /// <summary>The prefix used to turn a problem code into its absolute URN.</summary>
    public const string Prefix = "urn:monipay:error:";

    // Transport and host failures.

    /// <summary>The request body is not valid JSON.</summary>
    public static readonly ProblemType MalformedJson = new("malformed-json", HttpStatusCode.BadRequest);

    /// <summary>The request does not have the required JSON:API document shape.</summary>
    public static readonly ProblemType JsonApiDocumentInvalid = new("jsonapi-document-invalid", HttpStatusCode.BadRequest);

    /// <summary>The client accepts none of the supported response media types.</summary>
    public static readonly ProblemType NotAcceptable = new("not-acceptable", HttpStatusCode.NotAcceptable);

    /// <summary>The request media type is unsupported.</summary>
    public static readonly ProblemType UnsupportedMediaType = new("unsupported-media-type", HttpStatusCode.UnsupportedMediaType);

    /// <summary>One or more request attributes are invalid.</summary>
    public static readonly ProblemType Validation = new("validation", HttpStatusCode.UnprocessableEntity);

    /// <summary>A route identifier and a relationship identifier differ.</summary>
    public static readonly ProblemType ResourceIdentityMismatch = new("resource-identity-mismatch", HttpStatusCode.Conflict);

    /// <summary>The resource changed before this request could commit its update.</summary>
    public static readonly ProblemType ConcurrentModification = new("concurrent-modification", HttpStatusCode.Conflict);

    /// <summary>A rate or attempt limit was reached.</summary>
    public static readonly ProblemType RateLimited = new("rate-limited", HttpStatusCode.TooManyRequests);

    /// <summary>An unexpected server failure occurred.</summary>
    public static readonly ProblemType Internal = new("internal", HttpStatusCode.InternalServerError);

    // Sign-up failures.

    /// <summary>The sign-up is not in a state that permits the requested action.</summary>
    public static readonly ProblemType SignUpStateInvalid = new("signup-state-invalid", HttpStatusCode.Conflict);

    /// <summary>The verification code does not match.</summary>
    public static readonly ProblemType VerificationCodeInvalid = new("verification-code-invalid", HttpStatusCode.UnprocessableEntity);

    /// <summary>The current verification code has expired.</summary>
    public static readonly ProblemType VerificationCodeExpired = new("verification-code-expired", HttpStatusCode.Gone);

    /// <summary>The sign-up lifetime has ended.</summary>
    public static readonly ProblemType SignUpExpired = new("signup-expired", HttpStatusCode.Gone);

    /// <summary>The verification attempt limit was reached.</summary>
    public static readonly ProblemType SignUpAttemptLimit = new("signup-attempt-limit", HttpStatusCode.TooManyRequests);

    /// <summary>The verification-code resend limit was reached.</summary>
    public static readonly ProblemType SignUpResendLimit = new("signup-resend-limit", HttpStatusCode.TooManyRequests);

    /// <summary>The verification-code resend cooldown has not elapsed.</summary>
    public static readonly ProblemType SignUpResendTooSoon = new("signup-resend-too-soon", HttpStatusCode.TooManyRequests);

    /// <summary>The sign-up credential is invalid or expired.</summary>
    public static readonly ProblemType SignUpTokenInvalid = new("signup-token-invalid", HttpStatusCode.Unauthorized);

    /// <summary>A verified phone already belongs to a user.</summary>
    public static readonly ProblemType PhoneAlreadyRegistered = new("phone-already-registered", HttpStatusCode.Conflict);

    /// <summary>The normalized email already belongs to a user.</summary>
    public static readonly ProblemType EmailAlreadyRegistered = new("email-already-registered", HttpStatusCode.Conflict);

    /// <summary>The registration credential is invalid or expired.</summary>
    public static readonly ProblemType RegistrationTokenInvalid = new("registration-token-invalid", HttpStatusCode.Unauthorized);

    /// <summary>Verification-code delivery could not be queued.</summary>
    public static readonly ProblemType VerificationDeliveryUnavailable = new("verification-delivery-unavailable", HttpStatusCode.ServiceUnavailable);

    // Session failures.

    /// <summary>The access or refresh credential is invalid.</summary>
    public static readonly ProblemType SessionInvalid = new("session-invalid", HttpStatusCode.Unauthorized);

    /// <summary>A consumed refresh token was presented again.</summary>
    public static readonly ProblemType RefreshTokenReused = new("refresh-token-reused", HttpStatusCode.Unauthorized);
}
