namespace MoniPay.Kernel.Http;

public static class MoniPayHeaders
{
    public const string Client = "X-MoniPay-Client";

    public const string RetryAfter = "Retry-After";

    public const string SignUp = nameof(SignUp);

    public const string Registration = nameof(Registration);

    public const string Bearer = nameof(Bearer);
}
