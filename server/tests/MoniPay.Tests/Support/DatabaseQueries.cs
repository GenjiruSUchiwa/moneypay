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
}
