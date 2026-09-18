namespace MoniPay.Sessions;

public static class SessionMessageKeys
{
    public const string VerificationCodeSms = nameof(VerificationCodeSms);

    public const string SessionRevokedEmailSubject = nameof(SessionRevokedEmailSubject);

    public const string SessionRevokedEmailBody = nameof(SessionRevokedEmailBody);

    public const string RefreshTokenReuseDetectedSms = nameof(RefreshTokenReuseDetectedSms);

    public const string RefreshTokenReuseDetectedEmailSubject = nameof(RefreshTokenReuseDetectedEmailSubject);

    public const string RefreshTokenReuseDetectedEmailBody = nameof(RefreshTokenReuseDetectedEmailBody);
}
