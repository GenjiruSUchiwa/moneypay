namespace MoniPay.Kernel;

public readonly record struct EmailAddress(string Value)
{
    public string LookupValue => Value.ToLowerInvariant();

    public static bool TryNormalize(string? input, out EmailAddress email)
    {
        email = default;

        if (input is null)
        {
            return false;
        }

        string value = input.Trim();
        if (value.Length == 0 || value.Length > 254)
        {
            return false;
        }

        int atIndex = value.IndexOf('@');
        if (atIndex <= 0 || atIndex != value.LastIndexOf('@') || atIndex == value.Length - 1)
        {
            return false;
        }

        string localPart = value[..atIndex];
        string domainPart = value[(atIndex + 1)..];
        if (!IsValidLocalPart(localPart) || !IsValidDomainPart(domainPart))
        {
            return false;
        }

        string normalizedDomain = domainPart.ToLowerInvariant();
        string displayValue = string.Concat(localPart, "@", normalizedDomain);
        email = new(displayValue);
        return true;
    }

    private static bool IsValidLocalPart(string localPart)
    {
        foreach (char character in localPart)
        {
            if (character > '\u007F' || char.IsControl(character) || char.IsWhiteSpace(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidDomainPart(string domainPart)
    {
        int dotIndex = domainPart.IndexOf('.');
        if (dotIndex <= 0 || dotIndex == domainPart.Length - 1)
        {
            return false;
        }

        foreach (char character in domainPart)
        {
            if (char.IsControl(character) || char.IsWhiteSpace(character))
            {
                return false;
            }
        }

        return true;
    }
}
