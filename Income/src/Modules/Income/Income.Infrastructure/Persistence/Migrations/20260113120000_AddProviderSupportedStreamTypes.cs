using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Income.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddProviderSupportedStreamTypes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "supported_stream_types",
            schema: "income",
            table: "providers",
            type: "integer",
            nullable: false,
            defaultValue: 3); // 3 = Both (Income | Outcome)

        // Update exchange providers to only support Income
        migrationBuilder.Sql("""
            UPDATE income.providers
            SET supported_stream_types = 1
            WHERE connector_kind = 0
            """); // ConnectorKind.Syncable = 0 (exchanges)
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "supported_stream_types",
            schema: "income",
            table: "providers");
    }
}
