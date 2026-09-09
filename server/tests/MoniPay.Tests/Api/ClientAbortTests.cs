using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MoniPay.Api.Errors;
using MoniPay.Tests.Fakes;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Api;

/// <summary>
/// A client that leaves mid-request is answered by the framework's exception-handler middleware,
/// which never calls the host's handler for an aborted request: no body is written and no Error
/// event is logged. The handler no longer carries a branch for that case, so this pins the
/// guarantee where it now lives, with the host's own services and no timing.
/// </summary>
public sealed class ClientAbortTests(MoniPayApi api)
{
    [Fact]
    public async Task A_client_abort_writes_no_body_and_logs_no_error()
    {
        int before = api.Logs.Entries.Count;

        MemoryStream body = new();
        DefaultHttpContext context = new()
        {
            RequestServices = api.Services,
        };

        context.Request.Path = "/test/abort";
        context.RequestAborted = new CancellationToken(canceled: true);
        context.Response.Body = body;

        ApplicationBuilder builder = new(api.Services);
        builder.UseExceptionHandler();
        builder.Run(_ => throw new OperationCanceledException("the client left"));

        await builder.Build()(context);

        Assert.Equal(0, body.Length);
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, context.Response.StatusCode);
        Assert.DoesNotContain(api.Logs.Entries.Skip(before), IsHandlerError);
        Assert.DoesNotContain(
            api.Logs.Entries.Skip(before),
            entry => entry.Message.Contains("Unhandled exception", StringComparison.Ordinal));
    }

    private static bool IsHandlerError(RecordingLoggerProvider.LogEntry entry) =>
        entry.Category == typeof(MoniPayExceptionHandler).FullName && entry.Level >= LogLevel.Error;
}
