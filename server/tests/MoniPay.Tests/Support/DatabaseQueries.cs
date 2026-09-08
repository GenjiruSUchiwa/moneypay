using Npgsql;
using Xunit;

namespace MoniPay.Tests.Support;

/// <summary>Reads the test database directly, bypassing the API, to assert what PostgreSQL holds.</summary>
public static class DatabaseQueries
{
    public static async Task<IReadOnlyList<T>> QueryAsync<T>(
        this MoniPayApi api,
        string sql,
        Func<NpgsqlDataReader, T> read,
        params (string Name, string Value)[] parameters)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(read);

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using NpgsqlConnection connection = new(api.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using NpgsqlCommand command = new(sql, connection);
        foreach ((string name, string value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        List<T> rows = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(read(reader));
        }

        return rows;
    }

    /// <summary>
    /// Waits until some backend is blocked by the one PostgreSQL named <paramref name="processId"/>,
    /// so a test can act while a competing request waits for a row lock instead of sleeping.
    /// </summary>
    public static async Task WaitUntilBlockedAsync(
        this MoniPayApi api,
        int processId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);

        await using NpgsqlConnection observer = new(api.ConnectionString);
        await observer.OpenAsync(cancellationToken);
        await using NpgsqlCommand command = new(
            "SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE $1 = ANY(pg_blocking_pids(pid)));",
            observer);
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = processId });
        while (await command.ExecuteScalarAsync(cancellationToken) is not true)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
