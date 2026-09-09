using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MoniPay.Kernel;

namespace MoniPay.Notifications.Persistence;

internal sealed class TransactionCommitSignal(
    DeliverySignal signal,
    CarryingContexts carryingContexts) : DbTransactionInterceptor
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
        carryingContexts.Forget(eventData.Context);

    public override Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        carryingContexts.Forget(eventData.Context);
        return Task.CompletedTask;
    }

    private void Raise(DbContext? context)
    {
        if (carryingContexts.Holds(context))
        {
            carryingContexts.Forget(context);
            signal.Raise();
        }
    }
}
