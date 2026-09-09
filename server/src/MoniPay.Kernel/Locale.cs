namespace MoniPay.Kernel;

public readonly record struct Locale(string Value)
{
    public const string FrenchCameroonTag = "fr-CM";
    public const string FrenchTag = "fr";
    public const string EnglishTag = "en";

    public static Locale FrenchCameroon => new(FrenchCameroonTag);

    public static Locale French => new(FrenchTag);

    public static Locale English => new(EnglishTag);

    public static Locale Default => French;

    public static IReadOnlyList<string> SupportedTags { get; } = [FrenchCameroonTag, FrenchTag, EnglishTag];

    public static bool TryParse(string? value, out Locale locale)
    {
        foreach (string tag in SupportedTags)
        {
            if (string.Equals(tag, value?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                locale = new(tag);
                return true;
            }
        }

        locale = default;
        return false;
    }

    public override string ToString() => Value;
}
