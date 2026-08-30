using System.Text;

namespace MoniPay.Tests.Support;

/// <summary>Fixed key material for tests that validate required 32-byte secrets.</summary>
public static class TestKeys
{
    public static readonly string Signing = Encode("SigningKeyForMoniPayTests0000000");

    public static readonly string VerificationCode = Encode("VerificationCodeKeyForTests00000");

    public static readonly string SessionsPersonalData = Encode("SessionsPersonalDataKeyTests0000");

    public static readonly string UsersPersonalData = Encode("UsersPersonalDataKeyForTests0000");

    public static readonly string NotificationsData = Encode("NotificationsDataKeyForTests0000");

    private static string Encode(string value) =>
        Convert.ToBase64String(Encoding.ASCII.GetBytes(value));
}
