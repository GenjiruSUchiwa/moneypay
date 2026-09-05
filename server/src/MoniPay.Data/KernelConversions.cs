using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MoniPay.Kernel;

namespace MoniPay.Persistence;

/// <summary>
/// How the kernel's primitives are stored, declared once for every module: a typed identifier
/// is its <c>uuid</c>, a locale is its tag, a protected value is its ciphertext or its hash.
/// A module maps a column name and nothing else.
/// </summary>
internal static class KernelConversions
{
    public static void Apply(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<UserId>().HaveConversion<UserIdConverter>();
        configurationBuilder.Properties<SignUpId>().HaveConversion<SignUpIdConverter>();
        configurationBuilder.Properties<Locale>().HaveConversion<LocaleConverter>().HaveMaxLength(16);
        configurationBuilder.Properties<Ciphertext>().HaveConversion<CiphertextConverter>();
        configurationBuilder.Properties<LookupHash>().HaveConversion<LookupHashConverter>();
    }

    private sealed class UserIdConverter() : ValueConverter<UserId, Guid>(id => id.Value, value => new UserId(value));

    private sealed class SignUpIdConverter() : ValueConverter<SignUpId, Guid>(id => id.Value, value => new SignUpId(value));

    private sealed class LocaleConverter() : ValueConverter<Locale, string>(locale => locale.Value, value => new Locale(value));

    private sealed class CiphertextConverter() : ValueConverter<Ciphertext, string>(ciphertext => ciphertext.Value, value => new Ciphertext(value));

    private sealed class LookupHashConverter() : ValueConverter<LookupHash, byte[]>(hash => hash.Value, value => new LookupHash(value));
}
