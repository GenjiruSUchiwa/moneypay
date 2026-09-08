using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using MoniPay.Api.Http;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;

namespace MoniPay.Api.Errors;

/// <summary>
/// The one formatter for every error body, thrown or not. It preserves the caller's type, title
/// and status, fills in the request path and trace identifier, localizes a stable MoniPay code in
/// the request culture, and writes <c>application/problem+json</c> without a charset. It never
/// copies an exception message, and a 500 carries no detail.
/// </summary>
internal sealed class MoniPayProblemDetailsWriter(MoniPayProblemText text) : IProblemDetailsWriter
{
    /// <summary>The extension member that carries the validation items.</summary>
    internal const string ErrorsExtension = "errors";

    /// <summary>
    /// The type ASP.NET Core assigns when a problem has no explicit type. It is a framework
    /// default, not a caller's choice, so the writer replaces it with the stable MoniPay fallback.
    /// </summary>
    private const string FrameworkDefaultType = "https://tools.ietf.org/html/rfc9110";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <inheritdoc />
    /// <remarks>
    /// Always writable: an error response must use the Problem Details format even when the client
    /// did not ask for it. This is the deliberate error-format policy, not content negotiation.
    /// </remarks>
    public bool CanWrite(ProblemDetailsContext context) => true;

    /// <inheritdoc />
    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        HttpContext http = context.HttpContext;
        if (http.Response.HasStarted)
        {
            return;
        }

        ProblemDetails problem = context.ProblemDetails;
        int status = problem.Status ?? http.Response.StatusCode;
        http.Response.StatusCode = status;

        ApplyFields(problem, http, status);

        if (NoStoreConvention.AppliesTo(http))
        {
            NoStoreConvention.Apply(http.Response);
        }

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(problem, SerializerOptions);
        http.Response.ContentType = MoniPayMediaTypes.ProblemJson;
        http.Response.ContentLength = body.Length;

        if (!HttpMethods.IsHead(http.Request.Method))
        {
            await http.Response.Body.WriteAsync(body, http.RequestAborted);
        }
    }

    private void ApplyFields(ProblemDetails problem, HttpContext http, int status)
    {
        problem.Status = status;
        problem.Instance = http.Request.Path.Value;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? http.TraceIdentifier;

        if (string.IsNullOrEmpty(problem.Type)
            || problem.Type.StartsWith(FrameworkDefaultType, StringComparison.Ordinal))
        {
            ProblemType? fallback = AuthenticationCode(http, status)
                ?? MoniPayErrorTypes.ForStatus((HttpStatusCode)status);
            problem.Type = fallback?.Urn ?? "about:blank";
        }

        if (status == StatusCodes.Status500InternalServerError)
        {
            ApplyInternal(problem);
            return;
        }

        ApplyText(problem, MoniPayErrorTypes.CodeFromUrn(problem.Type));
    }

    private void ApplyInternal(ProblemDetails problem)
    {
        problem.Detail = null;
        problem.Extensions.Remove(ErrorsExtension);
        problem.Title = string.IsNullOrEmpty(problem.Title)
            ? text.Title(MoniPayErrorTypes.Internal.Code)
            : problem.Title;
    }

    private void ApplyText(ProblemDetails problem, string? code)
    {
        if (string.IsNullOrEmpty(problem.Title))
        {
            problem.Title = code is not null
                ? text.Title(code)
                : ReasonPhrases.GetReasonPhrase(problem.Status ?? 0);
        }

        if (problem.Detail is null && code is not null)
        {
            problem.Detail = text.Detail(code);
        }
    }

    private static ProblemType? AuthenticationCode(HttpContext http, int status)
    {
        if (status != StatusCodes.Status401Unauthorized)
        {
            return null;
        }

        return http.Items[MoniPayHttpContextItems.AuthenticationProblemCode] is string code
            ? new ProblemType(code, HttpStatusCode.Unauthorized)
            : null;
    }
}
