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
            migrationBuilder.DropPrimaryKey(
                name: "PK_userStatusReports",
                table: "userStatusReports");

            migrationBuilder.RenameTable(
                name: "userStatusReports",
                newName: "user_status_reports");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "user_status_reports",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "user_status_reports",
                newName: "is_notified");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_location_time",
                table: "user_status_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_notified_at",
                table: "user_status_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "notification_count",
                table: "user_status_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "offline_duration_minutes",
                table: "user_status_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_status_reports",
                table: "user_status_reports",
                column: "id");

            migrationBuilder.CreateTable(
                name: "admin_notifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    admin_user_id = table.Column<long>(type: "bigint", nullable: false),
                    notification_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    offline_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    is_sent = table.Column<bool>(type: "boolean", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delete_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_admin_notifications_users_admin_user_id",
                        column: x => x.admin_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admin_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceTokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    device_system = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_physical_device = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delete_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_DeviceTokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_status_reports_last_location_time",
                table: "user_status_reports",
                column: "last_location_time");

            migrationBuilder.CreateIndex(
                name: "IX_user_status_reports_last_notified_at",
                table: "user_status_reports",
                column: "last_notified_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_status_reports_user_id",
                table: "user_status_reports",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notifications_admin_user_id",
                table: "admin_notifications",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notifications_created_at",
                table: "admin_notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notifications_is_read",
                table: "admin_notifications",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notifications_is_sent",
                table: "admin_notifications",
                column: "is_sent");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notifications_user_id",
                table: "admin_notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_device_id",
                table: "DeviceTokens",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_user_id",
                table: "DeviceTokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_user_id_device_id",
                table: "DeviceTokens",
                columns: new[] { "user_id", "device_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_user_status_reports_users_user_id",
                table: "user_status_reports",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.RenameTable(
                name: "user_status_reports",
                newName: "userStatusReports");

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
