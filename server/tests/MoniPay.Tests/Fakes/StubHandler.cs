using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace MoniPay.Tests.Fakes;

/// <summary>A branch-free HTTP handler that records requests and returns one canned response.</summary>
public sealed class StubHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
{
    private readonly ConcurrentQueue<HttpRequestMessage> requests = new();

    public IReadOnlyList<HttpRequestMessage> Requests => requests.ToArray();

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        requests.Enqueue(request);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
    }
}
