using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoniPay.Persistence.Migrations;

/// <inheritdoc />
public partial class AddNotifications : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "notifications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                kind = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                recipient_ciphertext = table.Column<string>(type: "text", nullable: false),
                recipient_hint = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                subject_ciphertext = table.Column<string>(type: "text", nullable: true),
                body_ciphertext = table.Column<string>(type: "text", nullable: true),
                required = table.Column<bool>(type: "boolean", nullable: false),
                idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                attempts = table.Column<int>(type: "integer", nullable: false),
                next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                lease_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                provider_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                last_error_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_notifications", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_notifications_correlation_id",
            table: "notifications",
            column: "correlation_id");

        migrationBuilder.CreateIndex(
            name: "ix_notifications_idempotency_key",
            table: "notifications",
            column: "idempotency_key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_notifications_status_next_attempt_at",
            table: "notifications",
            columns: new[] { "status", "next_attempt_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "notifications");
    }
}
