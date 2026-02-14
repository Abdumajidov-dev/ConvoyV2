using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convoy.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserStoppedReportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS user_stopped_reports (
                    id BIGSERIAL PRIMARY KEY,
                    user_id INTEGER NOT NULL,
                    location_id BIGINT NOT NULL,
                    latitude DECIMAL(10, 8) NOT NULL,
                    longitude DECIMAL(11, 8) NOT NULL,
                    stopped_at TIMESTAMP WITH TIME ZONE NOT NULL,
                    stopped_duration_minutes INTEGER NOT NULL,
                    reason VARCHAR(500) NOT NULL DEFAULT '',
                    is_resolved BOOLEAN NOT NULL DEFAULT FALSE,
                    resolved_at TIMESTAMP WITH TIME ZONE,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP WITH TIME ZONE,
                    delete_at TIMESTAMP WITH TIME ZONE
                );

                CREATE INDEX IF NOT EXISTS idx_user_stopped_reports_user_id ON user_stopped_reports(user_id);
                CREATE INDEX IF NOT EXISTS idx_user_stopped_reports_location_id ON user_stopped_reports(location_id);
                CREATE INDEX IF NOT EXISTS idx_user_stopped_reports_created_at ON user_stopped_reports(created_at);
                CREATE INDEX IF NOT EXISTS idx_user_stopped_reports_is_resolved ON user_stopped_reports(is_resolved);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
