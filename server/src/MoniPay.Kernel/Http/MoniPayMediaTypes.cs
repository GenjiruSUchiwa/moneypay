namespace MoniPay.Kernel.Http;

public static class MoniPayMediaTypes
{
    public const string JsonApi = "application/vnd.api+json";

    public const string ProblemJson = "application/problem+json";

    public const string AnyContentType = "*/*";

    public const string Accept = $"{JsonApi}, {ProblemJson}";
}
