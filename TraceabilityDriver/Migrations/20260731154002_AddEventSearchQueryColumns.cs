using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilityDriver.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSearchQueryColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EPCIsReferenceOrChild",
                table: "EventSearchDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "EventSearchDocuments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransformationId",
                table: "EventSearchDocuments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_EventType",
                table: "EventSearchDocuments",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_TransformationId",
                table: "EventSearchDocuments",
                column: "TransformationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventSearchDocuments_EventType",
                table: "EventSearchDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EventSearchDocuments_TransformationId",
                table: "EventSearchDocuments");

            migrationBuilder.DropColumn(
                name: "EPCIsReferenceOrChild",
                table: "EventSearchDocuments");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "EventSearchDocuments");

            migrationBuilder.DropColumn(
                name: "TransformationId",
                table: "EventSearchDocuments");
        }
    }
}
