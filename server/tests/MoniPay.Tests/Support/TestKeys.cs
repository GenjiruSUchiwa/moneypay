using System.Text;
using Microsoft.AspNetCore.Hosting;
using MoniPay.Notifications;
using MoniPay.Sessions;
using MoniPay.Users;

namespace MoniPay.Tests.Support;

/// <summary>Fixed key material for tests that validate required 32-byte secrets.</summary>
public static class TestKeys
{
    public static readonly string Signing = Encode("SigningKeyForMoniPayTests0000000");

    public static readonly string VerificationCode = Encode("VerificationCodeKeyForTests00000");

    public static readonly string SessionsPersonalData = Encode("SessionsPersonalDataKeyTests0000");

    public static readonly string UsersPersonalData = Encode("UsersPersonalDataKeyForTests0000");

    public static readonly string NotificationsData = Encode("NotificationsDataKeyForTests0000");

    /// <summary>
    /// Every key the host refuses to start without. A test that probes one key calls this first
    /// and overrides that one setting afterwards; a new mandatory key is added here only.
    /// </summary>
    public static IWebHostBuilder UseTestKeys(this IWebHostBuilder builder)
    {
        builder.UseSetting(UsersOptions.Keys.PersonalDataKeyBase64, UsersPersonalData);
        builder.UseSetting(SessionsOptions.Keys.VerificationCodeKeyBase64, VerificationCode);
        builder.UseSetting(SessionsOptions.Keys.PersonalDataKeyBase64, SessionsPersonalData);
        builder.UseSetting(NotificationsOptions.Keys.DataKeyBase64, NotificationsData);
        return builder;
    }

    private static string Encode(string value) =>
        Convert.ToBase64String(Encoding.ASCII.GetBytes(value));
}
