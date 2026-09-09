namespace MoniPay.Kernel.Errors;

public sealed class ProviderUnavailableException : Exception
{
    public string ProviderName { get; }

    public string? ResultCode { get; }

    public ProviderUnavailableException(string providerName, string? resultCode = null, Exception? innerException = null)
        : base(null, innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerName);
        ProviderName = providerName;
        ResultCode = resultCode;
    }
}
