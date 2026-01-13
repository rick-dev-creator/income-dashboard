using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Income.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "income");

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "income",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    stream_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    stream_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "providers",
                schema: "income",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    connector_kind = table.Column<int>(type: "integer", nullable: false),
                    default_currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    sync_frequency = table.Column<int>(type: "integer", nullable: false),
                    config_schema = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "streams",
                schema: "income",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    provider_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    original_currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_fixed = table.Column<bool>(type: "boolean", nullable: false),
                    fixed_period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    encrypted_credentials = table.Column<string>(type: "text", nullable: true),
                    sync_state = table.Column<int>(type: "integer", nullable: false),
                    last_success_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    next_scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recurring_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    recurring_frequency = table.Column<int>(type: "integer", nullable: true),
                    recurring_start_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "snapshots",
                schema: "income",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    stream_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    original_amount = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    original_currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    usd_amount = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    rate_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    snapshot_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_snapshots_streams_stream_id",
                        column: x => x.stream_id,
                        principalSchema: "income",
                        principalTable: "streams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_created_at",
                schema: "income",
                table: "notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_is_read",
                schema: "income",
                table: "notifications",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_type",
                schema: "income",
                table: "notifications",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_providers_name",
                schema: "income",
                table: "providers",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_snapshots_date",
                schema: "income",
                table: "snapshots",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_snapshots_stream_date",
                schema: "income",
                table: "snapshots",
                columns: new[] { "stream_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_streams_category",
                schema: "income",
                table: "streams",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_streams_provider_id",
                schema: "income",
                table: "streams",
                column: "provider_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notifications",
                schema: "income");

            migrationBuilder.DropTable(
                name: "providers",
                schema: "income");

            migrationBuilder.DropTable(
                name: "snapshots",
                schema: "income");

            migrationBuilder.DropTable(
                name: "streams",
                schema: "income");
        }
    }
}
