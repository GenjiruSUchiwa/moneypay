using System.Data.Common;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MoniPay.Kernel;

namespace MoniPay.Notifications.Persistence;

/// <summary>
/// The transaction half of the commit signal: when a save that carried a notification commits
/// through an explicit transaction, the wake-up happens here — after the commit, never before.
/// A rolled-back transaction forgets the marker, so a later save on the same context cannot
/// signal for work that no longer exists.
/// </summary>
internal sealed class TransactionCommitSignal(
    DeliverySignal signal,
    ConditionalWeakTable<DbContext, object> carryingContexts) : DbTransactionInterceptor
{
    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData) =>
        Raise(eventData.Context);

    public override Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Raise(eventData.Context);
        return Task.CompletedTask;
    }

    public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData) =>
        Forget(eventData.Context);

    public override Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Forget(eventData.Context);
        return Task.CompletedTask;
    }

    private void Raise(DbContext? context)
    {
        if (context is not null && carryingContexts.TryGetValue(context, out _))
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
