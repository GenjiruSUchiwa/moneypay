namespace MoniPay.Kernel.Errors;

/// <summary>
/// Indicates that a provider-facing operation could not accept work. It names the provider and its
/// result code so the host can log them; it never carries user data.
/// </summary>
public sealed class ProviderUnavailableException : Exception
{
    /// <summary>The provider that refused the work.</summary>
    public string ProviderName { get; }

    /// <summary>The provider's own result code, when it returned one.</summary>
    public string? ResultCode { get; }

    public ProviderUnavailableException(string providerName, string? resultCode = null, Exception? innerException = null)
        : base(null, innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerName);
        ProviderName = providerName;
        ResultCode = resultCode;
    }
}
