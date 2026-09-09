using MoniPay.Kernel.Security;

namespace MoniPay.Users;

internal sealed class UsersOptions
{
    public const string SectionName = "MoniPay:Users";

    public string PersonalDataKeyBase64 { get; set; } = string.Empty;

    public byte[] PersonalDataKey => Base64Key.Decode(PersonalDataKeyBase64);

    public static class Keys
    {
        public const string PersonalDataKeyBase64 = $"{SectionName}:{nameof(PersonalDataKeyBase64)}";
    }
}
