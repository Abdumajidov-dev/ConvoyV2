using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Convoy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyDistanceReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_distance_reports",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    report_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_distance_meters = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_distance_km = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    location_count = table.Column<int>(type: "integer", nullable: false),
                    first_location_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_location_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delete_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_distance_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_daily_distance_reports_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_distance_reports_report_date",
                table: "daily_distance_reports",
                column: "report_date");

            migrationBuilder.CreateIndex(
                name: "IX_daily_distance_reports_user_id",
                table: "daily_distance_reports",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_daily_distance_reports_user_id_report_date",
                table: "daily_distance_reports",
                columns: new[] { "user_id", "report_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_distance_reports");
        }
    }
}
