using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convoy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoryFieldsToUserStatusReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "user_status_reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "note",
                table: "user_status_reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_change_type",
                table: "user_status_reports",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "user_status_reports");

            migrationBuilder.DropColumn(
                name: "note",
                table: "user_status_reports");

            migrationBuilder.DropColumn(
                name: "status_change_type",
                table: "user_status_reports");
        }
    }
}
