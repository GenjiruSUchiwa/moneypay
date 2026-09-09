using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MoniPay.Kernel;

namespace MoniPay.Notifications.Persistence;

internal sealed class NotificationCommitInterceptor(DeliverySignal signal)
    : SaveChangesInterceptor, IDbContextOptionsContributor
{
    private readonly CarryingContexts carryingContexts = new();

    void IDbContextOptionsContributor.Contribute(DbContextOptionsBuilder options) =>
        options.AddInterceptors(this, new TransactionCommitSignal(signal, carryingContexts));

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        carryingContexts.Mark(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        carryingContexts.Mark(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        RaiseWhenCommitted(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        RaiseWhenCommitted(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) =>
        carryingContexts.Forget(eventData.Context);

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        carryingContexts.Forget(eventData.Context);
        return Task.CompletedTask;
    }

    private void RaiseWhenCommitted(DbContext? context)
    {
        if (context is not null
            && context.Database.CurrentTransaction is null
            && carryingContexts.Holds(context))
        {
            carryingContexts.Forget(context);
            signal.Raise();
        }
    }
}
