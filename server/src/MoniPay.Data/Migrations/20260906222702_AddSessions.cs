using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoniPay.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSessions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                session_id = table.Column<Guid>(type: "uuid", nullable: false),
                token_digest = table.Column<byte[]>(type: "bytea", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                replaced_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_refresh_tokens", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "sessions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                device_id = table.Column<Guid>(type: "uuid", nullable: false),
                token_family_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revoke_reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sessions", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_expires_at",
            table: "refresh_tokens",
            column: "expires_at");

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_session_id_active",
            table: "refresh_tokens",
            column: "session_id",
            unique: true,
            filter: "used_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_token_digest",
            table: "refresh_tokens",
            column: "token_digest",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_sessions_token_family_id_device_id",
            table: "sessions",
            columns: new[] { "token_family_id", "device_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_sessions_user_id_revoked_at",
            table: "sessions",
            columns: new[] { "user_id", "revoked_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "refresh_tokens");

        migrationBuilder.DropTable(
            name: "sessions");
    }
}
