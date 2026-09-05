using MoniPay.Kernel.Security;

namespace MoniPay.Users;

/// <summary>
/// The module's configuration. The personal-data key is refused at startup rather than at the
/// first registration: a host that cannot encrypt must not come up.
/// </summary>
internal sealed class UsersOptions
{
    public const string SectionName = "MoniPay:Users";

    /// <summary>The 32-byte AES key protecting user personal data, base64-encoded.</summary>
    public string PersonalDataKeyBase64 { get; set; } = string.Empty;

    /// <summary>The decoded personal-data key. Valid only once the options are validated.</summary>
    public byte[] PersonalDataKey => Base64Key.Decode(PersonalDataKeyBase64);

    /// <summary>The configuration keys, declared so a mistyped key is a compile error.</summary>
    public static class Keys
    {
        public const string PersonalDataKeyBase64 = $"{SectionName}:{nameof(PersonalDataKeyBase64)}";
    }
}
