using System.Text;
using Microsoft.AspNetCore.Hosting;
using MoniPay.Notifications;
using MoniPay.Sessions;
using MoniPay.Users;

namespace MoniPay.Tests.Support;

public static class TestKeys
{
    public const string Issuer = "https://tests.monipay.example";

    public const string Audience = "monipay-mobile-tests";

    public static readonly string Signing = Encode("SigningKeyForMoniPayTests0000000");

    public static readonly string OtherSigning = Encode("OtherSigningKeyMoniPayTests00000");

    public static readonly string VerificationCode = Encode("VerificationCodeKeyForTests00000");

    public static readonly string SessionsPersonalData = Encode("SessionsPersonalDataKeyTests0000");

    public static readonly string UsersPersonalData = Encode("UsersPersonalDataKeyForTests0000");

    public static readonly string NotificationsData = Encode("NotificationsDataKeyForTests0000");

    public static IWebHostBuilder UseTestKeys(this IWebHostBuilder builder)
    {
        builder.UseSetting(UsersOptions.Keys.PersonalDataKeyBase64, UsersPersonalData);
        builder.UseSetting(SessionsOptions.Keys.VerificationCodeKeyBase64, VerificationCode);
        builder.UseSetting(SessionsOptions.Keys.PersonalDataKeyBase64, SessionsPersonalData);
        builder.UseSetting(SessionsOptions.Keys.SigningKeyBase64, Signing);
        builder.UseSetting(SessionsOptions.Keys.Issuer, Issuer);
        builder.UseSetting(SessionsOptions.Keys.Audience, Audience);
        builder.UseSetting(SessionsOptions.Keys.LegalTermsVersion, SignUpFlow.TermsVersion);
        builder.UseSetting(SessionsOptions.Keys.LegalPrivacyVersion, SignUpFlow.PrivacyVersion);
        builder.UseSetting(NotificationsOptions.Keys.DataKeyBase64, NotificationsData);
        return builder;
    }

    private static string Encode(string value) =>
        Convert.ToBase64String(Encoding.ASCII.GetBytes(value));
}
