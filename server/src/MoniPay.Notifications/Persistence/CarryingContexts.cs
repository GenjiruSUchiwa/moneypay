using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace MoniPay.Notifications.Persistence;

internal sealed class CarryingContexts
{
    private static readonly object Marker = new();

    private readonly ConditionalWeakTable<DbContext, object> contexts = new();

    public void Mark(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        contexts.Remove(context);
        contexts.Add(context, Marker);
    }

    public bool Holds(DbContext? context) =>
        context is not null && contexts.TryGetValue(context, out _);

    public void Forget(DbContext? context)
    {
        if (context is not null)
        {
            contexts.Remove(context);
        }
    }
}
