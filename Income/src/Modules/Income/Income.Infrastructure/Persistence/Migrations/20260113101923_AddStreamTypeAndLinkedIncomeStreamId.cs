using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Income.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStreamTypeAndLinkedIncomeStreamId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "linked_income_stream_id",
                schema: "income",
                table: "streams",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "stream_type",
                schema: "income",
                table: "streams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_streams_linked_income_stream_id",
                schema: "income",
                table: "streams",
                column: "linked_income_stream_id");

            migrationBuilder.CreateIndex(
                name: "ix_streams_stream_type",
                schema: "income",
                table: "streams",
                column: "stream_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_streams_linked_income_stream_id",
                schema: "income",
                table: "streams");

            migrationBuilder.DropIndex(
                name: "ix_streams_stream_type",
                schema: "income",
                table: "streams");

            migrationBuilder.DropColumn(
                name: "linked_income_stream_id",
                schema: "income",
                table: "streams");

            migrationBuilder.DropColumn(
                name: "stream_type",
                schema: "income",
                table: "streams");
        }
    }
}
