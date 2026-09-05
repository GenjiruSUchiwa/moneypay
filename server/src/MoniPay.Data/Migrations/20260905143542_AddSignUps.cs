using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoniPay.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSignUps : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "sign_ups",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                phone_ciphertext = table.Column<string>(type: "text", nullable: false),
                phone_lookup_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                code_digest = table.Column<byte[]>(type: "bytea", nullable: true),
                code_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                signup_token_digest = table.Column<byte[]>(type: "bytea", nullable: true),
                registration_token_digest = table.Column<byte[]>(type: "bytea", nullable: true),
                status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                failed_attempts = table.Column<int>(type: "integer", nullable: false),
                resend_count = table.Column<int>(type: "integer", nullable: false),
                can_resend_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                terms_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                privacy_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: true),
                bootstrap_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sign_ups", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_sign_ups_phone_lookup_hash",
            table: "sign_ups",
            column: "phone_lookup_hash",
            unique: true,
            filter: "status IN ('CodePending', 'PhoneVerified', 'Locked')");

        migrationBuilder.CreateIndex(
            name: "ix_sign_ups_registration_token_digest",
            table: "sign_ups",
            column: "registration_token_digest",
            unique: true,
            filter: "registration_token_digest IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_sign_ups_signup_token_digest",
            table: "sign_ups",
            column: "signup_token_digest",
            unique: true,
            filter: "signup_token_digest IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_sign_ups_status_expires_at",
            table: "sign_ups",
            columns: new[] { "status", "expires_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "sign_ups");
    }
}
