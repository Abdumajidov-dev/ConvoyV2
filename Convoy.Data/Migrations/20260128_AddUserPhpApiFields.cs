using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convoy.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPhpApiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add user_id column (PHP API worker_id)
            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "users",
                type: "integer",
                nullable: true);

            // Add branch_guid column
            migrationBuilder.AddColumn<string>(
                name: "branch_guid",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Add branch_name column
            migrationBuilder.AddColumn<string>(
                name: "branch_name",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // Add worker_guid column
            migrationBuilder.AddColumn<string>(
                name: "worker_guid",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Add position_id column
            migrationBuilder.AddColumn<int>(
                name: "position_id",
                table: "users",
                type: "integer",
                nullable: true);

            // Add image column
            migrationBuilder.AddColumn<string>(
                name: "image",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Add user_type column
            migrationBuilder.AddColumn<string>(
                name: "user_type",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Add role column
            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "idx_users_user_id",
                table: "users",
                column: "user_id",
                unique: false,
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_users_role",
                table: "users",
                column: "role",
                unique: false,
                filter: "role IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_users_phone",
                table: "users",
                column: "phone",
                unique: false,
                filter: "phone IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "idx_users_user_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_users_role",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_users_phone",
                table: "users");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "user_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "branch_guid",
                table: "users");

            migrationBuilder.DropColumn(
                name: "branch_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "worker_guid",
                table: "users");

            migrationBuilder.DropColumn(
                name: "position_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "image",
                table: "users");

            migrationBuilder.DropColumn(
                name: "user_type",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role",
                table: "users");
        }
    }
}
