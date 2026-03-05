using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convoy.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDailyDistanceReportsUserIdAndBranchGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_daily_distance_reports_users_user_id",
                table: "daily_distance_reports");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "daily_distance_reports",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "branch_guid",
                table: "daily_distance_reports",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_distance_reports_branch_guid",
                table: "daily_distance_reports",
                column: "branch_guid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_daily_distance_reports_branch_guid",
                table: "daily_distance_reports");

            migrationBuilder.DropColumn(
                name: "branch_guid",
                table: "daily_distance_reports");

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                table: "daily_distance_reports",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_daily_distance_reports_users_user_id",
                table: "daily_distance_reports",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
