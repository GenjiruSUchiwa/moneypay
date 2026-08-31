using MoniPay.Tests.Support;
using MoniPay.Users.Persistence;
using Xunit;

namespace MoniPay.Tests.Migrations;

/// <summary>
/// Asserts the shape PostgreSQL ended up with, not the shape the model asked for: the host
/// applies the migrations on start, so this reads <c>information_schema</c> after the fact.
/// </summary>
public sealed class SchemaTests(MoniPayApi api) : MoniPayApiTest(api)
{
    private sealed record Column(string Name, string DataType, bool Nullable, int? MaximumLength);

    [Fact]
    public async Task The_users_table_has_exactly_the_documented_columns()
    {
        Column[] expected =
        [
            new("id", "uuid", false, null),
            new("sign_up_id", "uuid", false, null),
            new("first_name_ciphertext", "text", false, null),
            new("last_name_ciphertext", "text", false, null),
            new("phone_ciphertext", "text", false, null),
            new("phone_lookup_hash", "bytea", false, null),
            new("email_ciphertext", "text", false, null),
            new("email_lookup_hash", "bytea", false, null),
            new("locale", "character varying", false, 16),
            new("created_at", "timestamp with time zone", false, null),
        ];

        Assert.Equal(expected.OrderBy(column => column.Name), await ColumnsOfAsync(UsersSchema.UsersTable));
    }

    [Fact]
    public async Task The_user_consents_table_has_exactly_the_documented_columns()
    {
        Column[] expected =
        [
            new("user_id", "uuid", false, null),
            new("document_kind", "character varying", false, 24),
            new("document_version", "character varying", false, 64),
            new("accepted_at", "timestamp with time zone", false, null),
        ];

        Assert.Equal(expected.OrderBy(column => column.Name), await ColumnsOfAsync(UsersSchema.UserConsentsTable));
    }

    [Theory]
    [InlineData(UsersSchema.UsersTable, UsersSchema.UsersPrimaryKey, "id")]
    [InlineData(UsersSchema.UserConsentsTable, UsersSchema.UserConsentsPrimaryKey, "user_id", "document_kind")]
    public async Task The_primary_key_is_named_by_the_module_and_spans_the_documented_columns(
        string table,
        string constraintName,
        params string[] columns)
    {
        IReadOnlyList<string> actual = await Api.QueryAsync(
            """
            SELECT key_column.column_name
            FROM information_schema.table_constraints AS constraint_metadata
            INNER JOIN information_schema.key_column_usage AS key_column
                ON key_column.constraint_name = constraint_metadata.constraint_name
                AND key_column.table_schema = constraint_metadata.table_schema
            WHERE constraint_metadata.table_schema = 'public'
                AND constraint_metadata.table_name = @table
                AND constraint_metadata.constraint_name = @constraint
                AND constraint_metadata.constraint_type = 'PRIMARY KEY'
            ORDER BY key_column.ordinal_position;
            """,
            reader => reader.GetString(0),
            ("table", table),
            ("constraint", constraintName));

        Assert.Equal(columns, actual);
    }

    [Theory]
    [InlineData(UsersSchema.SignUpIdUnique, "sign_up_id")]
    [InlineData(UsersSchema.PhoneLookupHashUnique, "phone_lookup_hash")]
    [InlineData(UsersSchema.EmailLookupHashUnique, "email_lookup_hash")]
    public async Task The_unique_index_exists_under_the_name_the_module_declares(string indexName, string column)
    {
        IReadOnlyList<string> definitions = await Api.QueryAsync(
            "SELECT indexdef FROM pg_indexes WHERE schemaname = 'public' AND indexname = @index;",
            reader => reader.GetString(0),
            ("index", indexName));

        string definition = Assert.Single(definitions);
        Assert.Equal($"CREATE UNIQUE INDEX {indexName} ON public.users USING btree ({column})", definition);
    }

    [Fact]
    public async Task Deleting_a_user_is_refused_while_its_consents_exist()
    {
        IReadOnlyList<string> rules = await Api.QueryAsync(
            """
            SELECT delete_rule FROM information_schema.referential_constraints
            WHERE constraint_schema = 'public' AND constraint_name = @constraint;
            """,
            reader => reader.GetString(0),
            ("constraint", UsersSchema.UserConsentsUserForeignKey));

        Assert.Equal("RESTRICT", Assert.Single(rules));
    }

    [Fact]
    public async Task No_column_uses_a_floating_point_type()
    {
        IReadOnlyList<string> offenders = await Api.QueryAsync(
            """
            SELECT table_name || '.' || column_name FROM information_schema.columns
            WHERE table_schema = 'public' AND data_type IN ('real', 'double precision');
            """,
            reader => reader.GetString(0));

        Assert.Empty(offenders);
    }

    private Task<IReadOnlyList<Column>> ColumnsOfAsync(string table) =>
        Api.QueryAsync(
            """
            SELECT column_name, data_type, is_nullable, character_maximum_length
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = @table
            ORDER BY column_name;
            """,
            reader => new Column(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2) == "YES",
                reader.IsDBNull(3) ? null : reader.GetInt32(3)),
            ("table", table));

}
