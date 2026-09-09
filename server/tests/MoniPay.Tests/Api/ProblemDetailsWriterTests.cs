using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MoniPay.Api.Errors;
using MoniPay.Kernel.Errors;
using MoniPay.Kernel.Http;
using MoniPay.Tests.Support;
using Xunit;

namespace MoniPay.Tests.Api;

public sealed class ProblemDetailsWriterTests(MoniPayApi api)
{
    private const int Unauthorized = StatusCodes.Status401Unauthorized;

    [Fact]
    public async Task An_explicit_problem_type_beats_the_authentication_code()
    {
        ProblemDetails problem = new() { Type = MoniPayErrorTypes.Validation.Urn, Status = Unauthorized };

        await WriteAsync(problem, MoniPayErrorTypes.SessionInvalid.Code);

        Assert.Equal(MoniPayErrorTypes.Validation.Urn, problem.Type);
    }

    [Fact]
    public async Task A_401_without_a_selected_code_keeps_the_generic_fallback()
    {
        ProblemDetails problem = new() { Status = Unauthorized };

        await WriteAsync(problem, code: null);

        Assert.Equal(MoniPayErrorTypes.Unauthorized.Urn, problem.Type);
    }

    private async Task WriteAsync(ProblemDetails problem, string? code)
    {
        HttpContext context = new DefaultHttpContext
        {
            RequestServices = api.Services,
        };

        context.Request.Path = "/test/secure";
        context.Response.StatusCode = Unauthorized;

        if (code is not null)
        {
            context.Items[MoniPayHttpContextItems.AuthenticationProblemCode] = code;
        }

        await api.Services.GetRequiredService<MoniPayProblemDetailsWriter>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
        });
    }
}
