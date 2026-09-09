using System.Net;

namespace MoniPay.Kernel.Errors;

public static class MoniPayErrorTypes
{
    public const string Prefix = "urn:monipay:error:";

    public static readonly ProblemType MalformedJson = new("malformed-json", HttpStatusCode.BadRequest);

    public static readonly ProblemType JsonApiDocumentInvalid = new("jsonapi-document-invalid", HttpStatusCode.BadRequest);

    public static readonly ProblemType NotAcceptable = new("not-acceptable", HttpStatusCode.NotAcceptable);

    public static readonly ProblemType UnsupportedMediaType = new("unsupported-media-type", HttpStatusCode.UnsupportedMediaType);

    public static readonly ProblemType Validation = new("validation", HttpStatusCode.UnprocessableEntity);

    public static readonly ProblemType ResourceIdentityMismatch = new("resource-identity-mismatch", HttpStatusCode.Conflict);

    public static readonly ProblemType ConcurrentModification = new("concurrent-modification", HttpStatusCode.Conflict);

    public static readonly ProblemType RateLimited = new("rate-limited", HttpStatusCode.TooManyRequests);

    public static readonly ProblemType Internal = new("internal", HttpStatusCode.InternalServerError);

    public static readonly ProblemType BadRequest = new("bad-request", HttpStatusCode.BadRequest);

    public static readonly ProblemType Unauthorized = new("unauthorized", HttpStatusCode.Unauthorized);

    public static readonly ProblemType Forbidden = new("forbidden", HttpStatusCode.Forbidden);

    public static readonly ProblemType NotFound = new("not-found", HttpStatusCode.NotFound);

    public static readonly ProblemType MethodNotAllowed = new("method-not-allowed", HttpStatusCode.MethodNotAllowed);

    public static readonly ProblemType ContentTooLarge = new("content-too-large", HttpStatusCode.RequestEntityTooLarge);

    public static readonly ProblemType ServiceUnavailable = new("service-unavailable", HttpStatusCode.ServiceUnavailable);

    public static readonly ProblemType SignUpStateInvalid = new("signup-state-invalid", HttpStatusCode.Conflict);

    public static readonly ProblemType VerificationCodeInvalid = new("verification-code-invalid", HttpStatusCode.UnprocessableEntity);

    public static readonly ProblemType VerificationCodeExpired = new("verification-code-expired", HttpStatusCode.Gone);

    public static readonly ProblemType SignUpExpired = new("signup-expired", HttpStatusCode.Gone);

    public static readonly ProblemType SignUpAttemptLimit = new("signup-attempt-limit", HttpStatusCode.TooManyRequests);

    public static readonly ProblemType SignUpResendLimit = new("signup-resend-limit", HttpStatusCode.TooManyRequests);

    public static readonly ProblemType SignUpResendTooSoon = new("signup-resend-too-soon", HttpStatusCode.TooManyRequests);

    public static readonly ProblemType SignUpTokenInvalid = new("signup-token-invalid", HttpStatusCode.Unauthorized);

    public static readonly ProblemType PhoneAlreadyRegistered = new("phone-already-registered", HttpStatusCode.Conflict);

    public static readonly ProblemType EmailAlreadyRegistered = new("email-already-registered", HttpStatusCode.Conflict);

    public static readonly ProblemType RegistrationTokenInvalid = new("registration-token-invalid", HttpStatusCode.Unauthorized);

    public static readonly ProblemType VerificationDeliveryUnavailable = new("verification-delivery-unavailable", HttpStatusCode.ServiceUnavailable);

    public static readonly ProblemType SessionInvalid = new("session-invalid", HttpStatusCode.Unauthorized);

    public static readonly ProblemType RefreshTokenReused = new("refresh-token-reused", HttpStatusCode.Unauthorized);

    private static readonly IReadOnlyDictionary<HttpStatusCode, ProblemType> FrameworkFallbacks =
        new Dictionary<HttpStatusCode, ProblemType>
        {
            [BadRequest.Status] = BadRequest,
            [Unauthorized.Status] = Unauthorized,
            [Forbidden.Status] = Forbidden,
            [NotFound.Status] = NotFound,
            [MethodNotAllowed.Status] = MethodNotAllowed,
            [NotAcceptable.Status] = NotAcceptable,
            [ContentTooLarge.Status] = ContentTooLarge,
            [UnsupportedMediaType.Status] = UnsupportedMediaType,
            [RateLimited.Status] = RateLimited,
            [Internal.Status] = Internal,
            [ServiceUnavailable.Status] = ServiceUnavailable,
        };

    public static ProblemType? ForStatus(HttpStatusCode status) => FrameworkFallbacks.GetValueOrDefault(status);

    public static string? CodeFromUrn(string? type) =>
        type is not null && type.StartsWith(Prefix, StringComparison.Ordinal)
            ? type[Prefix.Length..]
            : null;
}
