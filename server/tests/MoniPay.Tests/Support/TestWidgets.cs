using System.Net;
using System.Net.Http.Headers;
using System.Text;
using MoniPay.Kernel.Http;
using Xunit;

namespace MoniPay.Tests.Support;

public static class TestWidgets
{
    public const string ValidDocument = """{"data":{"type":"widgets","attributes":{"name":"W"}}}""";

    public static async Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        Uri route,
        string body,
        string? contentType = MoniPayMediaTypes.JsonApi,
        bool unknownLength = false)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, route)
        {
            Content = unknownLength
                ? UnknownLengthContent(body)
                : new StringContent(body, Encoding.UTF8),
        };
        request.Content.Headers.ContentType =
            contentType is null ? null : MediaTypeHeaderValue.Parse(contentType);

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static string DocumentOfSize(int size)
    {
        const string prefix = "{\"data\":{\"type\":\"widgets\",\"attributes\":{\"name\":\"";
        const string suffix = "\"}}}";
        int padding = size - prefix.Length - suffix.Length;

        return prefix + new string('a', padding) + suffix;
    }

    private static HttpContent UnknownLengthContent(string body) => new UnknownLengthHttpContent(body);

    private sealed class UnknownLengthHttpContent(string body) : HttpContent
    {
        private readonly byte[] bytes = Encoding.UTF8.GetBytes(body);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(bytes).AsTask();
    }
}
