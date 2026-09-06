using MoniPay.Notifications.Persistence;
using MoniPay.Sessions.Persistence;
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
    public async Task The_sign_ups_table_has_exactly_the_documented_columns()
    {
        Column[] expected =
        [
            new("id", "uuid", false, null),
            new("phone_ciphertext", "text", false, null),
            new("phone_lookup_hash", "bytea", false, null),
            new("locale", "character varying", false, 16),
            new("code_digest", "bytea", true, null),
            new("code_expires_at", "timestamp with time zone", true, null),
            new("signup_token_digest", "bytea", true, null),
            new("registration_token_digest", "bytea", true, null),
            new("status", "character varying", false, 24),
            new("failed_attempts", "integer", false, null),
            new("resend_count", "integer", false, null),
            new("can_resend_at", "timestamp with time zone", false, null),
            new("locked_until", "timestamp with time zone", true, null),
            new("expires_at", "timestamp with time zone", false, null),
            new("terms_version", "character varying", false, 64),
            new("privacy_version", "character varying", false, 64),
            new("user_id", "uuid", true, null),
            new("bootstrap_session_id", "uuid", true, null),
            new("version", "bigint", false, null),
            new("created_at", "timestamp with time zone", false, null),
            new("verified_at", "timestamp with time zone", true, null),
            new("completed_at", "timestamp with time zone", true, null),
        ];

        Assert.Equal(expected.OrderBy(column => column.Name), await ColumnsOfAsync(SessionsSchema.SignUpsTable));
    }

    [Fact]
    public async Task The_sessions_table_has_exactly_the_documented_columns()
    {
        Column[] expected =
        [
            new("id", "uuid", false, null),
            new("user_id", "uuid", false, null),
            new("device_id", "uuid", false, null),
            new("token_family_id", "uuid", false, null),
            new("version", "bigint", false, null),
            new("created_at", "timestamp with time zone", false, null),
            new("last_seen_at", "timestamp with time zone", false, null),
            new("revoked_at", "timestamp with time zone", true, null),
            new("revoke_reason", "character varying", true, 32),
        ];

        Assert.Equal(expected.OrderBy(column => column.Name), await ColumnsOfAsync(SessionsSchema.SessionsTable));
    }

    [Fact]
    public async Task The_refresh_tokens_table_has_exactly_the_documented_columns()
    {
        Column[] expected =
        [
            new("id", "uuid", false, null),
            new("session_id", "uuid", false, null),
            new("token_digest", "bytea", false, null),
            new("created_at", "timestamp with time zone", false, null),
            new("expires_at", "timestamp with time zone", false, null),
            new("used_at", "timestamp with time zone", true, null),
            new("replaced_by_id", "uuid", true, null),
        ];

        Assert.Equal(expected.OrderBy(column => column.Name), await ColumnsOfAsync(SessionsSchema.RefreshTokensTable));
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

    [Fact]
    public async Task The_notifications_table_has_exactly_the_documented_columns()
    {
        Column[] expected =
        [
            new("attempts", "integer", false, null),
            new("body_ciphertext", "text", true, null),
            new("channel", "character varying", false, 16),
            new("correlation_id", "uuid", false, null),
            new("created_at", "timestamp with time zone", false, null),
            new("expires_at", "timestamp with time zone", true, null),
            new("id", "uuid", false, null),
            new("idempotency_key", "character varying", false, 128),
            new("kind", "character varying", false, 48),
            new("last_error_code", "character varying", true, 64),
            new("lease_until", "timestamp with time zone", true, null),
            new("next_attempt_at", "timestamp with time zone", false, null),
            new("provider_reference", "character varying", true, 128),
            new("recipient_ciphertext", "text", false, null),
            new("recipient_hint", "character varying", false, 8),
            new("required", "boolean", false, null),
            new("sent_at", "timestamp with time zone", true, null),
            new("status", "character varying", false, 16),
            new("subject_ciphertext", "text", true, null),
        ];

        Assert.Equal(expected.OrderBy(column => column.Name), await ColumnsOfAsync(NotificationsSchema.NotificationsTable));
    }

    [Theory]
    [InlineData(UsersSchema.UsersTable, UsersSchema.UsersPrimaryKey, "id")]
    [InlineData(UsersSchema.UserConsentsTable, UsersSchema.UserConsentsPrimaryKey, "user_id", "document_kind")]
    [InlineData(SessionsSchema.SignUpsTable, SessionsSchema.SignUpsPrimaryKey, "id")]
    [InlineData(SessionsSchema.SessionsTable, SessionsSchema.SessionsPrimaryKey, "id")]
    [InlineData(SessionsSchema.RefreshTokensTable, SessionsSchema.RefreshTokensPrimaryKey, "id")]
    [InlineData(NotificationsSchema.NotificationsTable, NotificationsSchema.NotificationsPrimaryKey, "id")]
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
        string definition = await IndexDefinitionAsync(indexName);

        Assert.Equal($"CREATE UNIQUE INDEX {indexName} ON public.users USING btree ({column})", definition);
    }

    [Fact]
    public async Task The_cleanup_scan_index_spans_the_status_and_the_expiry()
    {
        string definition = await IndexDefinitionAsync(SessionsSchema.SignUpStatusExpiryIndex);

        Assert.Equal(
            $"CREATE INDEX {SessionsSchema.SignUpStatusExpiryIndex} ON public.sign_ups USING btree (status, expires_at)",
            definition);
    }

    [Fact]
    public async Task The_notification_claim_index_spans_the_status_and_the_next_attempt()
    {
        string definition = await IndexDefinitionAsync(NotificationsSchema.ClaimIndex);

        Assert.Equal(
            $"CREATE INDEX {NotificationsSchema.ClaimIndex} ON public.notifications USING btree (status, next_attempt_at)",
            definition);
    }

    [Fact]
    public async Task The_notification_idempotency_key_is_unique_under_the_name_the_module_declares()
    {
        string definition = await IndexDefinitionAsync(NotificationsSchema.IdempotencyKeyUnique);

        Assert.Equal(
            $"CREATE UNIQUE INDEX {NotificationsSchema.IdempotencyKeyUnique} ON public.notifications USING btree (idempotency_key)",
            definition);
    }

    [Fact]
    public async Task The_notification_correlation_index_supports_the_status_lookup()
    {
        string definition = await IndexDefinitionAsync(NotificationsSchema.CorrelationIdIndex);

        Assert.Equal(
            $"CREATE INDEX {NotificationsSchema.CorrelationIdIndex} ON public.notifications USING btree (correlation_id)",
            definition);
    }
    [Fact]
    public async Task The_active_workflow_index_is_unique_per_phone_over_the_nonterminal_statuses()
    {
        string definition = await IndexDefinitionAsync(SessionsSchema.SignUpPhoneActiveWorkflowUnique);

        Assert.StartsWith(
            $"CREATE UNIQUE INDEX {SessionsSchema.SignUpPhoneActiveWorkflowUnique} ON public.sign_ups USING btree (phone_lookup_hash) WHERE",
            definition,
            StringComparison.Ordinal);
        Assert.Contains("'CodePending'", definition, StringComparison.Ordinal);
        Assert.Contains("'PhoneVerified'", definition, StringComparison.Ordinal);
        Assert.Contains("'Locked'", definition, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(SessionsSchema.SignUpTokenDigestUnique, "signup_token_digest")]
    [InlineData(SessionsSchema.SignUpRegistrationTokenDigestUnique, "registration_token_digest")]
    public async Task The_token_digest_index_is_unique_while_the_credential_exists(string indexName, string column)
    {
        string definition = await IndexDefinitionAsync(indexName);

        Assert.StartsWith(
            $"CREATE UNIQUE INDEX {indexName} ON public.sign_ups USING btree ({column}) WHERE",
            definition,
            StringComparison.Ordinal);
        Assert.Contains($"{column} IS NOT NULL", definition, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(SessionsSchema.SessionUserRevokedIndex, "sessions", "(user_id, revoked_at)", false)]
    [InlineData(SessionsSchema.SessionFamilyDeviceUnique, "sessions", "(token_family_id, device_id)", true)]
    [InlineData(SessionsSchema.RefreshTokenDigestUnique, "refresh_tokens", "(token_digest)", true)]
    [InlineData(SessionsSchema.RefreshTokenExpiryIndex, "refresh_tokens", "(expires_at)", false)]
    public async Task The_session_index_exists_under_the_name_the_module_declares(
        string indexName,
        string table,
        string columns,
        bool unique)
    {
        string definition = await IndexDefinitionAsync(indexName);

        Assert.Equal(
            $"CREATE {(unique ? "UNIQUE " : string.Empty)}INDEX {indexName} ON public.{table} USING btree {columns}",
            definition);
    }

    [Fact]
    public async Task The_active_refresh_token_index_is_unique_per_session_while_unconsumed()
    {
        string definition = await IndexDefinitionAsync(SessionsSchema.RefreshTokenActivePerSessionUnique);

        Assert.Equal(
            $"CREATE UNIQUE INDEX {SessionsSchema.RefreshTokenActivePerSessionUnique} ON public.refresh_tokens USING btree (session_id) WHERE (used_at IS NULL)",
            definition);
    }

    [Fact]
    public async Task The_sessions_table_has_no_foreign_key()
    {
        IReadOnlyList<string> constraints = await Api.QueryAsync(
            """
            SELECT constraint_name FROM information_schema.table_constraints
            WHERE table_schema = 'public' AND table_name = @table AND constraint_type = 'FOREIGN KEY';
            """,
            reader => reader.GetString(0),
            ("table", SessionsSchema.SessionsTable));

        Assert.Empty(constraints);
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

    private async Task<string> IndexDefinitionAsync(string indexName)
    {
        IReadOnlyList<string> definitions = await Api.QueryAsync(
            "SELECT indexdef FROM pg_indexes WHERE schemaname = 'public' AND indexname = @index;",
            reader => reader.GetString(0),
            ("index", indexName));

        return Assert.Single(definitions);
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
