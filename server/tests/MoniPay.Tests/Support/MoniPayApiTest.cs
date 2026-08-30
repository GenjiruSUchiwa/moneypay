using Xunit;

namespace MoniPay.Tests.Support;

/// <summary>Common access to the shared MoniPay test host and its cancellation token.</summary>
public abstract class MoniPayApiTest(MoniPayApi api) : IDisposable
{
    protected MoniPayApi Api { get; } = api;

    protected HttpClient Client { get; } = api.CreateClient();

    protected CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public void Dispose() => Client.Dispose();
}
