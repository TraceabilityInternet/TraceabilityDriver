using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilityDriver.Migrations
{
    /// <inheritdoc />
    public partial class AddTracebackTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TracebackItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TracebackId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Created = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TracebackItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tracebacks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResolverUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedEpcs = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Errors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventsCreated = table.Column<int>(type: "int", nullable: false),
                    EventsUpdated = table.Column<int>(type: "int", nullable: false),
                    MasterDataCreated = table.Column<int>(type: "int", nullable: false),
                    MasterDataUpdated = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracebacks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TracebackItems_Traceback_Type_Item",
                table: "TracebackItems",
                columns: new[] { "TracebackId", "ItemType", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TracebackItems_TracebackId",
                table: "TracebackItems",
                column: "TracebackId");

            migrationBuilder.CreateIndex(
                name: "IX_Tracebacks_StartTime",
                table: "Tracebacks",
                column: "StartTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TracebackItems");

            migrationBuilder.DropTable(
                name: "Tracebacks");
        }
    }
}
