using Npgsql;
using Xunit;

namespace MoniPay.Tests.Support;

/// <summary>Queries application tables for accidentally persisted plaintext values.</summary>
public static class DatabaseAssertions
{
    private const string ColumnsQuery = """
        SELECT column_metadata.table_name, column_metadata.column_name
        FROM information_schema.columns AS column_metadata
        INNER JOIN information_schema.tables AS table_metadata
            ON table_metadata.table_schema = column_metadata.table_schema
            AND table_metadata.table_name = column_metadata.table_name
        WHERE column_metadata.table_schema = 'public'
            AND table_metadata.table_type = 'BASE TABLE'
            AND column_metadata.table_name <> '__EFMigrationsHistory'
        ORDER BY column_metadata.table_name, column_metadata.ordinal_position;
        """;

    public static async Task<IReadOnlyList<string>> RowsContainingAsync(
        this MoniPayApi api,
        string plaintext)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using NpgsqlConnection connection = new(api.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        List<(string Table, string Column)> columns = [];
        await using (NpgsqlCommand columnsCommand = new(ColumnsQuery, connection))
        await using (NpgsqlDataReader reader = await columnsCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        using NpgsqlCommandBuilder commandBuilder = new();
        List<string> matches = [];
        foreach ((string table, string column) in columns)
        {
            string tableIdentifier = commandBuilder.QuoteIdentifier(table);
            string columnIdentifier = commandBuilder.QuoteIdentifier(column);
            string query = $"SELECT EXISTS (SELECT 1 FROM {tableIdentifier} "
                + $"WHERE CAST({columnIdentifier} AS text) = @plaintext);";

            await using NpgsqlCommand command = new(query, connection);
            command.Parameters.AddWithValue("plaintext", plaintext);

            if (await command.ExecuteScalarAsync(cancellationToken) is true)
            {
                matches.Add($"{table}.{column}");
            }
        }

        return matches;
    }
}
