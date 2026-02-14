using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Convoy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStatusReportAndAdminNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ALL CHANGES ALREADY EXIST IN DATABASE
            // This migration was already applied manually, so we skip all operations
            // to avoid "already exists" errors
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_status_reports_users_user_id",
                table: "user_status_reports");

            migrationBuilder.DropTable(
                name: "admin_notifications");

            migrationBuilder.DropTable(
                name: "DeviceTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_status_reports",
                table: "user_status_reports");

            migrationBuilder.DropIndex(
                name: "IX_user_status_reports_last_location_time",
                table: "user_status_reports");

            migrationBuilder.DropIndex(
                name: "IX_user_status_reports_last_notified_at",
                table: "user_status_reports");

            migrationBuilder.DropIndex(
                name: "IX_user_status_reports_user_id",
                table: "user_status_reports");

            migrationBuilder.DropColumn(
                name: "last_location_time",
                table: "user_status_reports");

            migrationBuilder.DropColumn(
                name: "last_notified_at",
                table: "user_status_reports");

            migrationBuilder.DropColumn(
                name: "notification_count",
                table: "user_status_reports");

            migrationBuilder.DropColumn(
                name: "offline_duration_minutes",
                table: "user_status_reports");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "userStatusReports",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "is_notified",
                table: "userStatusReports",
                newName: "Status");

            migrationBuilder.AddPrimaryKey(
                name: "PK_userStatusReports",
                table: "userStatusReports",
                column: "id");
        }
    }
}
