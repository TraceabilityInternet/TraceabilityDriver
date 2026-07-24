using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilityDriver.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EPCISEvents",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EventJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BizStep = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EPCISEvents", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "EventSearchDocuments",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BizStep = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EventTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RecordTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EPC = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductGTIN = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationGLN = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PartyPGLN = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSearchDocuments", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogEvent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessId = table.Column<int>(type: "int", nullable: true),
                    ThreadId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterDataDocuments",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElementId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ElementType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ElementJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterDataDocuments", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SyncHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Memory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalItems = table.Column<int>(type: "int", nullable: false),
                    ItemsProcessed = table.Column<int>(type: "int", nullable: false),
                    EventsCreated = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EPCISEvents_EventId",
                table: "EPCISEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_Action",
                table: "EventSearchDocuments",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_BizStep",
                table: "EventSearchDocuments",
                column: "BizStep");

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_EPC",
                table: "EventSearchDocuments",
                column: "EPC");

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_EventId",
                table: "EventSearchDocuments",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_EventTime",
                table: "EventSearchDocuments",
                column: "EventTime");

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_LocationGLN",
                table: "EventSearchDocuments",
                column: "LocationGLN");

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_PartyPGLN",
                table: "EventSearchDocuments",
                column: "PartyPGLN");

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_ProductGTIN",
                table: "EventSearchDocuments",
                column: "ProductGTIN");

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_RecordTime",
                table: "EventSearchDocuments",
                column: "RecordTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EPCISEvents");

            migrationBuilder.DropTable(
                name: "EventSearchDocuments");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "MasterDataDocuments");

            migrationBuilder.DropTable(
                name: "SyncHistory");
        }
    }
}
