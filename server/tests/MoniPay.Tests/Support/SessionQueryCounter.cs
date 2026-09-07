using System.Data.Common;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MoniPay.Tests.Support;

/// <summary>
/// Counts the queries the authorization handler runs against the sessions table, so a test can
/// assert the active-session check hits the database exactly once per request — and never from
/// a cache.
/// </summary>
public sealed class SessionQueryCounter : DbCommandInterceptor
{
    private int sessionQueries;

    public int SessionQueries => Volatile.Read(ref sessionQueries);

    public void Reset() => Volatile.Write(ref sessionQueries, 0);

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Count(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Count(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void Count(DbCommand command)
    {
        if (command.CommandText.Contains("sessions", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref sessionQueries);
        }
    }
}
