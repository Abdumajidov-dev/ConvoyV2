using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convoy.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateDailyDistanceReportFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Function 1: upsert_daily_distance_report
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION upsert_daily_distance_report(
    p_user_id INTEGER,
    p_date DATE
)
RETURNS BIGINT AS $$
DECLARE
    v_report_id BIGINT;
    v_total_distance_meters DECIMAL(12, 2);
    v_location_count INTEGER;
    v_first_location_time TIMESTAMPTZ;
    v_last_location_time TIMESTAMPTZ;
    v_branch_guid VARCHAR(100);
BEGIN
    -- Get user's branch_guid from users table
    SELECT branch_guid INTO v_branch_guid
    FROM users
    WHERE user_id = p_user_id
    LIMIT 1;

    -- Calculate distance and location data for the given date
    SELECT
        COALESCE(SUM(distance_from_previous), 0),
        COUNT(*),
        MIN(recorded_at),
        MAX(recorded_at)
    INTO
        v_total_distance_meters,
        v_location_count,
        v_first_location_time,
        v_last_location_time
    FROM locations
    WHERE user_id = p_user_id
      AND recorded_at >= p_date::TIMESTAMPTZ
      AND recorded_at < (p_date + INTERVAL '1 day')::TIMESTAMPTZ;

    -- Insert or update the report
    INSERT INTO daily_distance_reports (
        user_id,
        branch_guid,
        report_date,
        total_distance_meters,
        total_distance_km,
        location_count,
        first_location_time,
        last_location_time,
        created_at,
        updated_at
    ) VALUES (
        p_user_id,
        v_branch_guid,
        p_date,
        v_total_distance_meters,
        v_total_distance_meters / 1000.0,
        v_location_count,
        v_first_location_time,
        v_last_location_time,
        NOW(),
        NOW()
    )
    ON CONFLICT (user_id, report_date)
    DO UPDATE SET
        branch_guid = EXCLUDED.branch_guid,
        total_distance_meters = EXCLUDED.total_distance_meters,
        total_distance_km = EXCLUDED.total_distance_km,
        location_count = EXCLUDED.location_count,
        first_location_time = EXCLUDED.first_location_time,
        last_location_time = EXCLUDED.last_location_time,
        updated_at = NOW()
    RETURNING id INTO v_report_id;

    RETURN v_report_id;
END;
$$ LANGUAGE plpgsql;
");

            // Function 2: generate_daily_reports_for_date
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION generate_daily_reports_for_date(
    p_date DATE
)
RETURNS TABLE (
    user_id INTEGER,
    report_id BIGINT,
    distance_km DECIMAL
) AS $$
DECLARE
    v_user_record RECORD;
BEGIN
    -- Get all unique users who have location data for the given date
    FOR v_user_record IN
        SELECT DISTINCT l.user_id
        FROM locations l
        WHERE l.recorded_at >= p_date::TIMESTAMPTZ
          AND l.recorded_at < (p_date + INTERVAL '1 day')::TIMESTAMPTZ
    LOOP
        -- Generate report for each user
        user_id := v_user_record.user_id;
        report_id := upsert_daily_distance_report(v_user_record.user_id, p_date);

        -- Get the generated distance
        SELECT total_distance_km INTO distance_km
        FROM daily_distance_reports
        WHERE daily_distance_reports.user_id = v_user_record.user_id
          AND report_date = p_date;

        RETURN NEXT;
    END LOOP;

    RETURN;
END;
$$ LANGUAGE plpgsql;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop functions in reverse order
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS generate_daily_reports_for_date(DATE);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS upsert_daily_distance_report(INTEGER, DATE);");
        }
    }
}
