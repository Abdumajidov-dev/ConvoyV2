using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Convoy.Api.Controllers;

/// <summary>
/// Temporary controller for database maintenance tasks
/// </summary>
[ApiController]
[Route("api/db_maintenance")]
// [Authorize] // Temporarily disabled for testing
public class DatabaseMaintenanceController : ControllerBase
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<DatabaseMaintenanceController> _logger;

    public DatabaseMaintenanceController(
        NpgsqlConnection connection,
        ILogger<DatabaseMaintenanceController> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    /// <summary>
    /// Create or update daily distance report PostgreSQL functions
    /// POST /api/db_maintenance/create_daily_distance_functions
    /// </summary>
    [HttpPost("create_daily_distance_functions")]
    public async Task<IActionResult> CreateDailyDistanceFunctions()
    {
        try
        {
            _logger.LogInformation("Creating daily distance report functions...");

            // Drop existing functions first
            var dropFunctionsSql = @"
DROP FUNCTION IF EXISTS upsert_daily_distance_report(BIGINT, DATE);
DROP FUNCTION IF EXISTS upsert_daily_distance_report(INTEGER, DATE);
DROP FUNCTION IF EXISTS generate_daily_reports_for_date(DATE);";

            // Function 1: upsert_daily_distance_report
            var function1Sql = @"
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
    -- Get user's branch_guid
    SELECT branch_guid INTO v_branch_guid
    FROM users
    WHERE user_id = p_user_id
    LIMIT 1;

    -- Calculate distance
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

    -- Insert or update
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
$$ LANGUAGE plpgsql;";

            // Function 2: generate_daily_reports_for_date
            var function2Sql = @"
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
    FOR v_user_record IN
        SELECT DISTINCT l.user_id
        FROM locations l
        WHERE l.recorded_at >= p_date::TIMESTAMPTZ
          AND l.recorded_at < (p_date + INTERVAL '1 day')::TIMESTAMPTZ
    LOOP
        user_id := v_user_record.user_id;
        report_id := upsert_daily_distance_report(v_user_record.user_id, p_date);

        SELECT total_distance_km INTO distance_km
        FROM daily_distance_reports
        WHERE daily_distance_reports.user_id = v_user_record.user_id
          AND report_date = p_date;

        RETURN NEXT;
    END LOOP;

    RETURN;
END;
$$ LANGUAGE plpgsql;";

            // Open connection
            if (_connection.State != System.Data.ConnectionState.Open)
                await _connection.OpenAsync();

            // Drop existing functions first
            using (var cmdDrop = _connection.CreateCommand())
            {
                cmdDrop.CommandText = dropFunctionsSql;
                await cmdDrop.ExecuteNonQueryAsync();
                _logger.LogInformation("Dropped existing functions");
            }

            // Execute Function 1
            using (var cmd1 = _connection.CreateCommand())
            {
                cmd1.CommandText = function1Sql;
                await cmd1.ExecuteNonQueryAsync();
                _logger.LogInformation("✅ Function 'upsert_daily_distance_report' created successfully");
            }

            // Execute Function 2
            using (var cmd2 = _connection.CreateCommand())
            {
                cmd2.CommandText = function2Sql;
                await cmd2.ExecuteNonQueryAsync();
                _logger.LogInformation("✅ Function 'generate_daily_reports_for_date' created successfully");
            }

            return Ok(new
            {
                status = true,
                message = "PostgreSQL functions created successfully!",
                data = new
                {
                    functions_created = new[]
                    {
                        "upsert_daily_distance_report(p_user_id INTEGER, p_date DATE)",
                        "generate_daily_reports_for_date(p_date DATE)"
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PostgreSQL functions");
            return StatusCode(500, new
            {
                status = false,
                message = "Error creating functions: " + ex.Message,
                data = (object?)null
            });
        }
    }
}
