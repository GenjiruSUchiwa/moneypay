using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace MoniPay.Notifications.Persistence;

/// <summary>
/// The "this context's latest save carried a new notification" fact the module's two
/// interceptors share. The save interceptor marks the context before the save; consuming the
/// marker removes it and raises, so exactly one commit raises for one notification — a
/// rollback, a failed save or a later unrelated commit can never signal for work that is gone.
/// The interceptors are singletons, which is why the fact lives in a table keyed by context.
/// </summary>
internal sealed class CarryingContexts
{
    private static readonly object Marker = new();

    private readonly ConditionalWeakTable<DbContext, object> contexts = new();

    /// <summary>Marks <paramref name="context"/>'s current save as carrying a notification.</summary>
    public void Mark(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        contexts.Remove(context);
        contexts.Add(context, Marker);
    }

    /// <summary>Reports whether the context carries a notification, without consuming the marker.</summary>
    public bool Holds(DbContext? context) =>
        context is not null && contexts.TryGetValue(context, out _);

    /// <summary>Consumes the marker: a context whose marker was taken never signals again.</summary>
    public void Forget(DbContext? context)
    {
        if (context is not null)
        {
            contexts.Remove(context);
        }
    }
}
