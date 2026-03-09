-- ==============================================================================
-- RECALCULATE DISTANCE FROM PREVIOUS FOR ALL LOCATIONS
-- ==============================================================================
-- Bu script barcha locationlarning distance_from_previous qiymatini qaytadan
-- hisoblaydi. Bu muammo uchun: ba'zi locationlarning distance'i NULL yoki 0.

-- Haversine distance formula (meters)
CREATE OR REPLACE FUNCTION calculate_distance(
    lat1 DECIMAL, lon1 DECIMAL,
    lat2 DECIMAL, lon2 DECIMAL
) RETURNS DECIMAL AS $$
DECLARE
    r DECIMAL := 6371000; -- Earth radius in meters
    dlat DECIMAL;
    dlon DECIMAL;
    a DECIMAL;
    c DECIMAL;
    distance DECIMAL;
BEGIN
    dlat := RADIANS(lat2 - lat1);
    dlon := RADIANS(lon2 - lon1);

    a := SIN(dlat / 2) * SIN(dlat / 2) +
         COS(RADIANS(lat1)) * COS(RADIANS(lat2)) *
         SIN(dlon / 2) * SIN(dlon / 2);

    c := 2 * ATAN2(SQRT(a), SQRT(1 - a));
    distance := r * c;

    RETURN distance;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- Function: Recalculate distance for a specific user and date range
CREATE OR REPLACE FUNCTION recalculate_distances_for_user_and_date(
    p_user_id INTEGER,
    p_start_date DATE,
    p_end_date DATE
) RETURNS TABLE (
    updated_count BIGINT,
    total_distance_meters DECIMAL
) AS $$
DECLARE
    v_prev_location RECORD;
    v_curr_location RECORD;
    v_distance DECIMAL;
    v_updated_count BIGINT := 0;
    v_total_distance DECIMAL := 0;
BEGIN
    -- Get all locations for this user in date range, ordered by time
    FOR v_curr_location IN
        SELECT id, user_id, recorded_at, latitude, longitude
        FROM locations
        WHERE user_id = p_user_id
          AND recorded_at >= p_start_date::TIMESTAMPTZ
          AND recorded_at < (p_end_date + INTERVAL '1 day')::TIMESTAMPTZ
        ORDER BY recorded_at ASC
    LOOP
        -- Calculate distance from previous location
        IF v_prev_location IS NOT NULL THEN
            v_distance := calculate_distance(
                v_prev_location.latitude, v_prev_location.longitude,
                v_curr_location.latitude, v_curr_location.longitude
            );

            -- Update distance_from_previous
            UPDATE locations
            SET distance_from_previous = v_distance
            WHERE id = v_curr_location.id
              AND recorded_at = v_curr_location.recorded_at;

            v_updated_count := v_updated_count + 1;
            v_total_distance := v_total_distance + v_distance;
        ELSE
            -- First location - set distance to 0
            UPDATE locations
            SET distance_from_previous = 0
            WHERE id = v_curr_location.id
              AND recorded_at = v_curr_location.recorded_at;
        END IF;

        -- Save current as previous for next iteration
        v_prev_location := v_curr_location;
    END LOOP;

    updated_count := v_updated_count;
    total_distance_meters := v_total_distance;
    RETURN NEXT;
END;
$$ LANGUAGE plpgsql;

-- Function: Recalculate for ALL users on a specific date
CREATE OR REPLACE FUNCTION recalculate_all_distances_for_date(
    p_date DATE
) RETURNS TABLE (
    user_id INTEGER,
    updated_count BIGINT,
    total_distance_km DECIMAL
) AS $$
DECLARE
    v_user_record RECORD;
    v_result RECORD;
BEGIN
    -- Get all unique users who have location data for the given date
    FOR v_user_record IN
        SELECT DISTINCT l.user_id
        FROM locations l
        WHERE l.recorded_at >= p_date::TIMESTAMPTZ
          AND l.recorded_at < (p_date + INTERVAL '1 day')::TIMESTAMPTZ
        ORDER BY l.user_id
    LOOP
        -- Recalculate for this user
        SELECT * INTO v_result
        FROM recalculate_distances_for_user_and_date(
            v_user_record.user_id,
            p_date,
            p_date
        );

        user_id := v_user_record.user_id;
        updated_count := v_result.updated_count;
        total_distance_km := ROUND(v_result.total_distance_meters / 1000, 2);

        RETURN NEXT;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- ==============================================================================
-- USAGE EXAMPLES
-- ==============================================================================

-- Example 1: Recalculate for specific user and date
-- SELECT * FROM recalculate_distances_for_user_and_date(8558, '2026-03-05', '2026-03-05');

-- Example 2: Recalculate for all users on 2026-03-05
-- SELECT * FROM recalculate_all_distances_for_date('2026-03-05');

-- Example 3: After recalculation, regenerate daily reports
-- SELECT * FROM generate_daily_reports_for_date('2026-03-05');

-- ==============================================================================
-- TEST: Check if function created successfully
-- ==============================================================================
SELECT
    routine_name,
    routine_type
FROM information_schema.routines
WHERE routine_schema = 'public'
  AND routine_name IN ('calculate_distance', 'recalculate_distances_for_user_and_date', 'recalculate_all_distances_for_date')
ORDER BY routine_name;
