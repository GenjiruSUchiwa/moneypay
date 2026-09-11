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

    public const string EmailBaseUrl = "https://email.tests";

    public const string EmailApiKey = "monipay-tests-email-api-key";

    public const string EmailFromAddress = "no-reply@tests.monipay.example";

    public const string SmsBaseUrl = "https://sms-tests.monipay.example";

    public const string SmsApiKey = "sms-tests-inert";

    public const string SmsSenderId = "MoniPay";

    public static IWebHostBuilder UseTestKeys(this IWebHostBuilder builder)
    {
        foreach ((string key, string value) in Keys)
        {
            builder.UseSetting(key, value);
        }

        return builder;
    }

    private static readonly (string Key, string Value)[] Keys =
    [
        (UsersOptions.Keys.PersonalDataKeyBase64, UsersPersonalData),
        (SessionsOptions.Keys.VerificationCodeKeyBase64, VerificationCode),
        (SessionsOptions.Keys.PersonalDataKeyBase64, SessionsPersonalData),
        (SessionsOptions.Keys.SigningKeyBase64, Signing),
        (SessionsOptions.Keys.Issuer, Issuer),
        (SessionsOptions.Keys.Audience, Audience),
        (SessionsOptions.Keys.LegalTermsVersion, SignUpFlow.TermsVersion),
        (SessionsOptions.Keys.LegalPrivacyVersion, SignUpFlow.PrivacyVersion),
        (NotificationsOptions.Keys.DataKeyBase64, NotificationsData),
        (NotificationsOptions.Keys.EmailBaseUrl, EmailBaseUrl),
        (NotificationsOptions.Keys.EmailApiKey, EmailApiKey),
        (NotificationsOptions.Keys.EmailFromAddress, EmailFromAddress),
        (NotificationsOptions.Keys.SmsBaseUrl, SmsBaseUrl),
        (NotificationsOptions.Keys.SmsApiKey, SmsApiKey),
        (NotificationsOptions.Keys.SmsSenderId, SmsSenderId),
    ];

    private static string Encode(string value) =>
        Convert.ToBase64String(Encoding.ASCII.GetBytes(value));
}
