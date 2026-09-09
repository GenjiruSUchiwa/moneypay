using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Validation;

namespace MoniPay.Api.Errors;

/// <summary>
/// The single exception-to-problem mapping. It never copies an exception message into the
/// response: the writer resolves localized text from the stable code. It never sees a client
/// abort: the framework's exception-handler middleware answers an <c>OperationCanceledException</c>
/// whose request was aborted itself, so no body and no Error event are written for it.
/// </summary>
internal sealed class MoniPayExceptionHandler(
    IProblemDetailsService problemDetails,
    MoniPayProblemText text,
    ILogger<MoniPayExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);

        ProblemDetails problem = Map(exception);

        if (exception is RefusalException { RetryAfter: { } delay })
        {
            context.Response.Headers.RetryAfter = RetryAfterHeader.Format(delay);
        }

        Log(context, exception, MoniPayErrorTypes.CodeFromUrn(problem.Type) ?? MoniPayErrorTypes.Internal.Code);

        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
        });

        return true;
    }

    private ProblemDetails Map(Exception exception) => exception switch
    {
        ValidationException validation => Validation(validation),
        RefusalException refusal => Refusal(refusal),
        ProviderUnavailableException => MoniPay(MoniPayErrorTypes.VerificationDeliveryUnavailable),
        DbUpdateConcurrencyException => MoniPay(MoniPayErrorTypes.ConcurrentModification),
        BadHttpRequestException badRequest => MoniPay(
            MoniPayErrorTypes.ForStatus((HttpStatusCode)badRequest.StatusCode) ?? MoniPayErrorTypes.Internal),
        _ => MoniPay(MoniPayErrorTypes.Internal),
    };

    private ProblemDetails Validation(ValidationException exception)
    {
        ValidationProblemItem[] errors =
        [
            .. exception.Failures.Select(failure =>
                new ValidationProblemItem(text.FailureDetail(failure.Code), failure.Pointer)),
        ];

        ProblemDetails problem = MoniPay(MoniPayErrorTypes.Validation);
        problem.Extensions[MoniPayProblemDetailsWriter.ErrorsExtension] = errors;

        return problem;
    }

    private ProblemDetails Refusal(RefusalException exception)
    {
        ProblemDetails problem = MoniPay(exception.Type);

        if (exception.Pointers is { Count: > 0 } pointers)
        {
            string detail = text.Detail(exception.Type.Code) ?? text.Title(exception.Type.Code);
            ValidationProblemItem[] errors =
            [
                .. pointers.Select(pointer => new ValidationProblemItem(detail, pointer)),
            ];

            problem.Extensions[MoniPayProblemDetailsWriter.ErrorsExtension] = errors;
        }

        return problem;
    }

    private static ProblemDetails MoniPay(ProblemType type) => new()
    {
        Type = type.Urn,
        Status = (int)type.Status,
    };

    private void Log(HttpContext context, Exception exception, string code)
    {
        string route = RouteOf(context);

        switch (exception)
        {
            case ValidationException validation:
                MoniPayErrorLog.ValidationRefused(
                    logger,
                    route,
                    string.Join(',', validation.Failures.Select(failure => failure.Code)));
                break;
            case ProviderUnavailableException provider:
                MoniPayErrorLog.ProviderUnavailable(logger, provider.ProviderName, provider.ResultCode);
                break;
            case DbUpdateConcurrencyException concurrency:
                MoniPayErrorLog.ConcurrencyConflict(
                    logger,
                    route,
                    concurrency.Entries.FirstOrDefault()?.Metadata.Name ?? "unknown");
                break;
            case RefusalException:
            case BadHttpRequestException:
                MoniPayErrorLog.RequestRefused(logger, route, code);
                break;
            default:
                MoniPayErrorLog.UnhandledException(logger, exception, route);
                break;
        }
    }

    /// <summary>
    /// The endpoint name of the route that threw, or its path when it has none. The framework
    /// clears the current endpoint before it calls a handler, so the original one is read from
    /// the feature it saved for exactly this purpose.
    /// </summary>
    private static string RouteOf(HttpContext context) =>
        context.Features.Get<IExceptionHandlerFeature>()?.Endpoint?.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
        ?? context.Request.Path.Value
        ?? "unknown";
}
