using System.Data.Common;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MoniPay.Kernel;

namespace MoniPay.Notifications.Persistence;

/// <summary>
/// The commit signal of the outbox: it raises <see cref="DeliverySignal"/> exactly when a
/// committed change set carried a new notification. Raised earlier, the worker would wake to
/// find nothing; raised while a transaction is still open, it would wake before the row is
/// visible to anyone else.
/// </summary>
/// <remarks>
/// <c>MoniPay.Data</c> builds the shared options from every registered
/// <see cref="IDbContextOptionsContributor"/>, so the data project never references this
/// module. The interceptors are singletons, which is why the "this save carried a
/// notification" fact lives in a table keyed by context rather than in a field.
/// </remarks>
internal sealed class NotificationCommitInterceptor(DeliverySignal signal)
    : SaveChangesInterceptor, IDbContextOptionsContributor
{
    private static readonly object CarriedNotification = new();

    private readonly ConditionalWeakTable<DbContext, object> carryingContexts = new();

    void IDbContextOptionsContributor.Contribute(DbContextOptionsBuilder options) =>
        options.AddInterceptors(this, new TransactionCommitSignal(signal, carryingContexts));

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Remember(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Remember(eventData.Context);
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

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => Forget(eventData.Context);

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Forget(eventData.Context);
        return Task.CompletedTask;
    }

    private void Remember(DbContext? context)
    {
        if (context is null
            || !context.ChangeTracker.Entries<Domain.Notification>()
                .Any(entry => entry.State == EntityState.Added))
        {
            return;
        }

        carryingContexts.Remove(context);
        carryingContexts.Add(context, CarriedNotification);
    }

    private void RaiseWhenCommitted(DbContext? context)
    {
        if (context is null || !carryingContexts.TryGetValue(context, out _))
        {
            return;
        }

        if (context.Database.CurrentTransaction is null)
        {
            carryingContexts.Remove(context);
            signal.Raise();
        }
    }

    private void Forget(DbContext? context)
    {
        if (context is not null)
        {
            carryingContexts.Remove(context);
        }
    }
}
