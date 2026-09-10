using System.Collections.Concurrent;
using System.Net;
using System.Text;
using MoniPay.Notifications.Channels;

namespace MoniPay.Tests.Fakes;

public sealed class StubHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
{
    private readonly ConcurrentQueue<HttpRequestMessage> requests = new();
    private readonly ConcurrentQueue<RequestSnapshot> snapshots = new();

    public IReadOnlyList<RequestSnapshot> Snapshots => snapshots.ToArray();

    public IReadOnlyList<HttpRequestMessage> Requests => requests.ToArray();

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? Behavior { get; set; }

    public sealed record RequestSnapshot(
        string Method,
        Uri? Uri,
        string? Authorization,
        string? IdempotencyKey,
        string? ContentType,
        string Body);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        requests.Enqueue(request);
        string bodyText = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        snapshots.Enqueue(new RequestSnapshot(
            request.Method.Method,
            request.RequestUri,
            request.Headers.Authorization?.ToString(),
            request.Headers.TryGetValues(BirdSmsChannel.IdempotencyKeyHeader, out IEnumerable<string>? keys)
                ? string.Join(",", keys)
                : null,
            request.Content?.Headers.ContentType?.ToString(),
            bodyText));

        if (Behavior is not null)
        {
            return await Behavior(request, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }
}
