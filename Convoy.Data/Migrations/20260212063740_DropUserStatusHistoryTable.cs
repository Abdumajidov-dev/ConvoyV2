using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Convoy.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropUserStatusHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_status_history");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_status_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    additional_info = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delete_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_location_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notification_sent = table.Column<bool>(type: "boolean", nullable: false),
                    notification_threshold = table.Column<int>(type: "integer", nullable: true),
                    offline_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    status_change_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_status_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_status_history_created_at",
                table: "user_status_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_status_history_status_change_type",
                table: "user_status_history",
                column: "status_change_type");

            migrationBuilder.CreateIndex(
                name: "IX_user_status_history_user_id",
                table: "user_status_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_status_history_user_id_created_at",
                table: "user_status_history",
                columns: new[] { "user_id", "created_at" });
        }
    }
}
