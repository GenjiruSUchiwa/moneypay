using MoniPay.Tests.Support;
using MoniPay.Users.Persistence;
using Npgsql;
using Xunit;

namespace MoniPay.Tests.Migrations;

/// <summary>
/// Asserts the shape PostgreSQL ended up with, not the shape the model asked for: the host
/// applies the migrations on start, so this reads <c>information_schema</c> after the fact.
/// </summary>
public sealed class SchemaTests(MoniPayApi api) : MoniPayApiTest(api)
{
    [Theory]
    [InlineData(UsersConstraints.UsersTable, "id", "uuid", false)]
    [InlineData(UsersConstraints.UsersTable, "sign_up_id", "uuid", false)]
    [InlineData(UsersConstraints.UsersTable, "first_name_ciphertext", "text", false)]
    [InlineData(UsersConstraints.UsersTable, "last_name_ciphertext", "text", false)]
    [InlineData(UsersConstraints.UsersTable, "phone_ciphertext", "text", false)]
    [InlineData(UsersConstraints.UsersTable, "phone_lookup_hash", "bytea", false)]
    [InlineData(UsersConstraints.UsersTable, "email_ciphertext", "text", false)]
    [InlineData(UsersConstraints.UsersTable, "email_lookup_hash", "bytea", false)]
    [InlineData(UsersConstraints.UsersTable, "locale", "character varying", false)]
    [InlineData(UsersConstraints.UsersTable, "created_at", "timestamp with time zone", false)]
    [InlineData(UsersConstraints.UserConsentsTable, "user_id", "uuid", false)]
    [InlineData(UsersConstraints.UserConsentsTable, "document_kind", "character varying", false)]
    [InlineData(UsersConstraints.UserConsentsTable, "document_version", "character varying", false)]
    [InlineData(UsersConstraints.UserConsentsTable, "accepted_at", "timestamp with time zone", false)]
    public async Task The_column_exists_with_its_documented_type(
        string table,
        string column,
        string dataType,
        bool nullable)
    {
        (string DataType, bool Nullable)? actual = await ReadColumnAsync(table, column);

        Assert.NotNull(actual);
        Assert.Equal(dataType, actual.Value.DataType);
        Assert.Equal(nullable, actual.Value.Nullable);
    }

    [Fact]
    public async Task The_users_table_carries_no_kyc_flag()
    {
        Assert.Null(await ReadColumnAsync(UsersConstraints.UsersTable, "kyc_verified"));
    }

    [Theory]
    [InlineData(UsersConstraints.UsersTable, "locale", 16)]
    [InlineData(UsersConstraints.UserConsentsTable, "document_kind", 24)]
    [InlineData(UsersConstraints.UserConsentsTable, "document_version", 64)]
    public async Task The_string_column_is_bounded(string table, string column, int maximumLength)
    {
        object? actual = await ScalarAsync(
            """
            SELECT character_maximum_length FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = @table AND column_name = @column;
            """,
            ("table", table),
            ("column", column));

        Assert.Equal(maximumLength, Assert.IsType<int>(actual));
    }

    [Theory]
    [InlineData(UsersConstraints.SignUpIdUnique)]
    [InlineData(UsersConstraints.PhoneLookupHashUnique)]
    [InlineData(UsersConstraints.EmailLookupHashUnique)]
    public async Task The_unique_index_exists_under_the_name_the_module_declares(string indexName)
    {
        object? definition = await ScalarAsync(
            "SELECT indexdef FROM pg_indexes WHERE schemaname = 'public' AND indexname = @index;",
            ("index", indexName));

        Assert.Contains("CREATE UNIQUE INDEX", Assert.IsType<string>(definition), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_user_consents_table_is_keyed_by_user_and_document()
    {
        IReadOnlyList<string> keyColumns = await PrimaryKeyColumnsAsync(UsersConstraints.UserConsentsTable);

        Assert.Equal(["user_id", "document_kind"], keyColumns);
    }

    [Fact]
    public async Task No_column_uses_a_floating_point_type()
    {
        await using NpgsqlConnection connection = new(Api.ConnectionString);
        await connection.OpenAsync(Cancellation);
        await using NpgsqlCommand command = new(
            """
            SELECT table_name || '.' || column_name FROM information_schema.columns
            WHERE table_schema = 'public' AND data_type IN ('real', 'double precision');
            """,
            connection);

        List<string> offenders = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(Cancellation);
        while (await reader.ReadAsync(Cancellation))
        {
            offenders.Add(reader.GetString(0));
        }

        Assert.Empty(offenders);
    }

    private async Task<(string DataType, bool Nullable)?> ReadColumnAsync(string table, string column)
    {
        await using NpgsqlConnection connection = new(Api.ConnectionString);
        await connection.OpenAsync(Cancellation);
        await using NpgsqlCommand command = new(
            """
            SELECT data_type, is_nullable FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = @table AND column_name = @column;
            """,
            connection);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(Cancellation);
        if (!await reader.ReadAsync(Cancellation))
        {
            return null;
        }

        return (reader.GetString(0), reader.GetString(1) == "YES");
    }

    private async Task<IReadOnlyList<string>> PrimaryKeyColumnsAsync(string table)
    {
        await using NpgsqlConnection connection = new(Api.ConnectionString);
        await connection.OpenAsync(Cancellation);
        await using NpgsqlCommand command = new(
            """
            SELECT key_column.column_name
            FROM information_schema.table_constraints AS constraint_metadata
            INNER JOIN information_schema.key_column_usage AS key_column
                ON key_column.constraint_name = constraint_metadata.constraint_name
                AND key_column.table_schema = constraint_metadata.table_schema
            WHERE constraint_metadata.table_schema = 'public'
                AND constraint_metadata.table_name = @table
                AND constraint_metadata.constraint_type = 'PRIMARY KEY'
            ORDER BY key_column.ordinal_position;
            """,
            connection);
        command.Parameters.AddWithValue("table", table);

        List<string> columns = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(Cancellation);
        while (await reader.ReadAsync(Cancellation))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private async Task<object?> ScalarAsync(string sql, params (string Name, string Value)[] parameters)
    {
        await using NpgsqlConnection connection = new(Api.ConnectionString);
        await connection.OpenAsync(Cancellation);
        await using NpgsqlCommand command = new(sql, connection);
        foreach ((string name, string value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return await command.ExecuteScalarAsync(Cancellation);
    }
}
