using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceabilityDriver.Migrations
{
    /// <inheritdoc />
    public partial class AddDeploymentVersionAndEventKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EPCISEvents_EventId",
                table: "EPCISEvents");

            migrationBuilder.AddColumn<string>(
                name: "DeploymentVersion",
                table: "SyncHistory",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ElementId",
                table: "MasterDataDocuments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DeploymentVersion",
                table: "MasterDataDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeploymentVersion",
                table: "EventSearchDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventKey",
                table: "EventSearchDocuments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeploymentVersion",
                table: "EPCISEvents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventKey",
                table: "EPCISEvents",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncHistory_DeploymentVersion_EndTime",
                table: "SyncHistory",
                columns: new[] { "DeploymentVersion", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_MasterDataDocuments_ElementId_DeploymentVersion",
                table: "MasterDataDocuments",
                columns: new[] { "ElementId", "DeploymentVersion" },
                unique: true,
                filter: "[DeploymentVersion] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_DeploymentVersion",
                table: "EventSearchDocuments",
                column: "DeploymentVersion");

            migrationBuilder.CreateIndex(
                name: "IX_EventSearchDocuments_EventKey",
                table: "EventSearchDocuments",
                column: "EventKey");

            migrationBuilder.CreateIndex(
                name: "IX_EPCISEvents_EventId",
                table: "EPCISEvents",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EPCISEvents_EventKey_DeploymentVersion",
                table: "EPCISEvents",
                columns: new[] { "EventKey", "DeploymentVersion" },
                unique: true,
                filter: "[EventKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncHistory_DeploymentVersion_EndTime",
                table: "SyncHistory");

            migrationBuilder.DropIndex(
                name: "IX_MasterDataDocuments_ElementId_DeploymentVersion",
                table: "MasterDataDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EventSearchDocuments_DeploymentVersion",
                table: "EventSearchDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EventSearchDocuments_EventKey",
                table: "EventSearchDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EPCISEvents_EventId",
                table: "EPCISEvents");

            migrationBuilder.DropIndex(
                name: "IX_EPCISEvents_EventKey_DeploymentVersion",
                table: "EPCISEvents");

            migrationBuilder.DropColumn(
                name: "DeploymentVersion",
                table: "SyncHistory");

            migrationBuilder.DropColumn(
                name: "DeploymentVersion",
                table: "MasterDataDocuments");

            migrationBuilder.DropColumn(
                name: "DeploymentVersion",
                table: "EventSearchDocuments");

            migrationBuilder.DropColumn(
                name: "EventKey",
                table: "EventSearchDocuments");

            migrationBuilder.DropColumn(
                name: "DeploymentVersion",
                table: "EPCISEvents");

            migrationBuilder.DropColumn(
                name: "EventKey",
                table: "EPCISEvents");

            migrationBuilder.AlterColumn<string>(
                name: "ElementId",
                table: "MasterDataDocuments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.CreateIndex(
                name: "IX_EPCISEvents_EventId",
                table: "EPCISEvents",
                column: "EventId",
                unique: true);
        }
    }
}
