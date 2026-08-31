namespace MoniPay.Kernel;

/// <summary>
/// A language the product speaks, as a BCP-47 tag from a closed list. A stored locale drives
/// which translation a message picks, so an unsupported tag is refused at the edge rather than
/// silently falling back later.
/// </summary>
public readonly record struct Locale(string Value)
{
    public const string FrenchCameroonTag = "fr-CM";
    public const string FrenchTag = "fr";
    public const string EnglishTag = "en";

    public static Locale FrenchCameroon => new(FrenchCameroonTag);

    public static Locale French => new(FrenchTag);

    public static Locale English => new(EnglishTag);

    /// <summary>The default when a client states no preference: the product's users read French.</summary>
    public static Locale Default => French;

    /// <summary>Every tag the product answers in, most specific first.</summary>
    public static IReadOnlyList<string> SupportedTags { get; } = [FrenchCameroonTag, FrenchTag, EnglishTag];

    /// <summary>Attempts to parse a tag, case-insensitively, against the supported list.</summary>
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

    /// <inheritdoc />
    public override string ToString() => Value;
}
