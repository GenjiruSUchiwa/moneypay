using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoniPay.Persistence.Migrations;

/// <inheritdoc />
public partial class AddUsers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                sign_up_id = table.Column<Guid>(type: "uuid", nullable: false),
                first_name_ciphertext = table.Column<string>(type: "text", nullable: false),
                last_name_ciphertext = table.Column<string>(type: "text", nullable: false),
                locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                phone_ciphertext = table.Column<string>(type: "text", nullable: false),
                phone_lookup_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                email_ciphertext = table.Column<string>(type: "text", nullable: false),
                email_lookup_hash = table.Column<byte[]>(type: "bytea", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "user_consents",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                document_kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                document_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_consents", x => new { x.user_id, x.document_kind });
                table.ForeignKey(
                    name: "fk_user_consents_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_users_email_lookup_hash",
            table: "users",
            column: "email_lookup_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_users_phone_lookup_hash",
            table: "users",
            column: "phone_lookup_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_users_sign_up_id",
            table: "users",
            column: "sign_up_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "user_consents");

        migrationBuilder.DropTable(
            name: "users");
    }
}
