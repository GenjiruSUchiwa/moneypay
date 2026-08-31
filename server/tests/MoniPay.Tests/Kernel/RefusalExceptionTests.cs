using System.Net;
using MoniPay.Kernel.Errors;
using Xunit;

namespace MoniPay.Tests.Kernel;

public sealed class RefusalExceptionTests
{
    [Fact]
    public void A_refusal_preserves_its_type_retry_delay_and_pointers()
    {
        TimeSpan retryAfter = TimeSpan.FromSeconds(30);
        string[] pointers = ["/data/attributes/verificationCode"];

        RefusalException exception = new(MoniPayErrorTypes.SignUpAttemptLimit, retryAfter, pointers);

        Assert.Same(MoniPayErrorTypes.SignUpAttemptLimit, exception.Type);
        Assert.Equal(HttpStatusCode.TooManyRequests, exception.Type.Status);
        Assert.Equal(retryAfter, exception.RetryAfter);
        Assert.Equal(pointers, exception.Pointers);
    }

    [Fact]
    public void A_refusal_copies_pointers_before_exposing_them()
    {
        string[] pointers = ["/data/attributes/email"];
        RefusalException exception = new(MoniPayErrorTypes.EmailAlreadyRegistered, pointers: pointers);

        pointers[0] = "/data/attributes/phone";

        string[] preservedPointers = exception.Pointers?.ToArray() ?? [];
        Assert.Equal("/data/attributes/email", Assert.Single(preservedPointers));
    }

    [Fact]
    public void A_refusal_message_does_not_contain_user_data()
    {
        const string userData = "user-phone-237699123456";
        RefusalException exception = new(MoniPayErrorTypes.Validation, pointers: [userData]);

        Assert.DoesNotContain(userData, exception.Message);
    }

    [Fact]
    public void A_negative_retry_delay_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RefusalException(MoniPayErrorTypes.RateLimited, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void An_empty_problem_code_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new ProblemType(string.Empty, HttpStatusCode.Conflict));
    }

    [Fact]
    public void A_problem_type_exposes_its_urn()
    {
        Assert.Equal("urn:monipay:error:session-invalid", MoniPayErrorTypes.SessionInvalid.Urn);
    }

    [Fact]
    public void A_provider_failure_names_the_provider_and_its_result_code()
    {
        InvalidOperationException inner = new("queue full");

        ProviderUnavailableException exception = new("campay", "QUEUE_FULL", inner);

        Assert.Equal("campay", exception.ProviderName);
        Assert.Equal("QUEUE_FULL", exception.ResultCode);
        Assert.Same(inner, exception.InnerException);
        Assert.Throws<ArgumentException>(() => new ProviderUnavailableException(string.Empty));
    }

    [Fact]
    public void Problem_types_match_the_http_contract()
    {
        (ProblemType Actual, string Code, HttpStatusCode Status)[] problemTypes =
        [
            (MoniPayErrorTypes.MalformedJson, "malformed-json", HttpStatusCode.BadRequest),
            (MoniPayErrorTypes.JsonApiDocumentInvalid, "jsonapi-document-invalid", HttpStatusCode.BadRequest),
            (MoniPayErrorTypes.NotAcceptable, "not-acceptable", HttpStatusCode.NotAcceptable),
            (MoniPayErrorTypes.UnsupportedMediaType, "unsupported-media-type", HttpStatusCode.UnsupportedMediaType),
            (MoniPayErrorTypes.Validation, "validation", HttpStatusCode.UnprocessableEntity),
            (MoniPayErrorTypes.ResourceIdentityMismatch, "resource-identity-mismatch", HttpStatusCode.Conflict),
            (MoniPayErrorTypes.ConcurrentModification, "concurrent-modification", HttpStatusCode.Conflict),
            (MoniPayErrorTypes.RateLimited, "rate-limited", HttpStatusCode.TooManyRequests),
            (MoniPayErrorTypes.Internal, "internal", HttpStatusCode.InternalServerError),
            (MoniPayErrorTypes.SignUpStateInvalid, "signup-state-invalid", HttpStatusCode.Conflict),
            (MoniPayErrorTypes.VerificationCodeInvalid, "verification-code-invalid", HttpStatusCode.UnprocessableEntity),
            (MoniPayErrorTypes.VerificationCodeExpired, "verification-code-expired", HttpStatusCode.Gone),
            (MoniPayErrorTypes.SignUpExpired, "signup-expired", HttpStatusCode.Gone),
            (MoniPayErrorTypes.SignUpAttemptLimit, "signup-attempt-limit", HttpStatusCode.TooManyRequests),
            (MoniPayErrorTypes.SignUpResendLimit, "signup-resend-limit", HttpStatusCode.TooManyRequests),
            (MoniPayErrorTypes.SignUpResendTooSoon, "signup-resend-too-soon", HttpStatusCode.TooManyRequests),
            (MoniPayErrorTypes.SignUpTokenInvalid, "signup-token-invalid", HttpStatusCode.Unauthorized),
            (MoniPayErrorTypes.PhoneAlreadyRegistered, "phone-already-registered", HttpStatusCode.Conflict),
            (MoniPayErrorTypes.EmailAlreadyRegistered, "email-already-registered", HttpStatusCode.Conflict),
            (MoniPayErrorTypes.RegistrationTokenInvalid, "registration-token-invalid", HttpStatusCode.Unauthorized),
            (MoniPayErrorTypes.VerificationDeliveryUnavailable, "verification-delivery-unavailable", HttpStatusCode.ServiceUnavailable),
            (MoniPayErrorTypes.SessionInvalid, "session-invalid", HttpStatusCode.Unauthorized),
            (MoniPayErrorTypes.RefreshTokenReused, "refresh-token-reused", HttpStatusCode.Unauthorized),
        ];

        foreach ((ProblemType actual, string code, HttpStatusCode status) in problemTypes)
        {
            Assert.Equal(code, actual.Code);
            Assert.Equal(status, actual.Status);
        }
    }
}
