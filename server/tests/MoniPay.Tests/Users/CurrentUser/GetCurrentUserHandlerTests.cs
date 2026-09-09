using Microsoft.Extensions.DependencyInjection;
using MoniPay.Kernel;
using MoniPay.Kernel.Errors;
using MoniPay.Tests.Support;
using MoniPay.Users.Features.CurrentUser;
using Xunit;

namespace MoniPay.Tests.Users.CurrentUser;

/// <summary>
/// The current-user handler on its own: what a missing row and a canceled read do, without HTTP.
/// </summary>
public sealed class GetCurrentUserHandlerTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Fact]
    public async Task The_handler_refuses_the_credential_for_a_missing_user_instead_of_returning_empty()
    {
        RefusalException refusal = await Assert.ThrowsAsync<RefusalException>(
            () => Api.InScopeAsync<GetCurrentUserHandler, CurrentUserView>(
                (handler, cancellationToken) => handler.HandleAsync(UserId.New(), cancellationToken)));

        Assert.Equal(MoniPayErrorTypes.SessionInvalid.Code, refusal.Type.Code);
    }

    [Fact]
    public async Task The_handler_honors_a_canceled_token()
    {
        await using AsyncServiceScope scope = Api.Services.CreateAsyncScope();
        GetCurrentUserHandler handler = scope.ServiceProvider.GetRequiredService<GetCurrentUserHandler>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.HandleAsync(UserId.New(), new CancellationToken(canceled: true)));
    }
}
