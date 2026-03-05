-- Migration: Fix daily_distance_reports.user_id to reference users.user_id instead of users.id
-- Date: 2026-02-28
-- Purpose: Change FK from internal DB ID to external PHP API worker_id

-- Step 1: Drop existing foreign key constraint (if exists)
ALTER TABLE IF EXISTS daily_distance_reports
    DROP CONSTRAINT IF EXISTS fk_daily_distance_reports_user;

ALTER TABLE IF EXISTS daily_distance_reports
    DROP CONSTRAINT IF EXISTS daily_distance_reports_user_id_fkey;

-- Step 2: Add branch_guid column for filtering
ALTER TABLE daily_distance_reports
    ADD COLUMN IF NOT EXISTS branch_guid VARCHAR(100);

-- Step 3: Update existing data (if any exists)
-- Copy user_id from users table based on existing FK relationship
-- This assumes UserId currently stores users.id
UPDATE daily_distance_reports ddr
SET user_id = u.user_id,
    branch_guid = u.branch_guid
FROM users u
WHERE ddr.user_id = u.id AND u.user_id IS NOT NULL;

-- Step 4: Create index on users.user_id for better join performance
CREATE INDEX IF NOT EXISTS idx_users_user_id ON users(user_id);

-- Step 5: Add foreign key constraint to users.user_id
-- NOTE: We don't add FK constraint because users.user_id can be NULL
-- Instead, we handle this in application layer

-- Step 6: Create index on daily_distance_reports for filtering
CREATE INDEX IF NOT EXISTS idx_daily_distance_reports_user_id
    ON daily_distance_reports(user_id);

CREATE INDEX IF NOT EXISTS idx_daily_distance_reports_branch_guid
    ON daily_distance_reports(branch_guid);

CREATE INDEX IF NOT EXISTS idx_daily_distance_reports_report_date
    ON daily_distance_reports(report_date);

CREATE INDEX IF NOT EXISTS idx_daily_distance_reports_user_date
    ON daily_distance_reports(user_id, report_date);

-- Step 7: Drop and recreate the upsert function to use users.user_id
CREATE OR REPLACE FUNCTION upsert_daily_distance_report(
    p_user_id INTEGER,  -- Changed from BIGINT to INTEGER
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

-- Step 8: Drop and recreate the generate function
CREATE OR REPLACE FUNCTION generate_daily_reports_for_date(
    p_date DATE
)
RETURNS TABLE (
    user_id INTEGER,  -- Changed from BIGINT to INTEGER
    report_id BIGINT,
    distance_km DECIMAL
) AS $$
DECLARE
    v_user_record RECORD;
BEGIN
    -- Get all users who have location data for the given date
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

-- Step 9: Add unique constraint on (user_id, report_date) if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'daily_distance_reports_user_id_report_date_key'
    ) THEN
        ALTER TABLE daily_distance_reports
            ADD CONSTRAINT daily_distance_reports_user_id_report_date_key
            UNIQUE (user_id, report_date);
    END IF;
END $$;

-- Verification queries
SELECT 'Migration completed successfully!' AS status;

-- Show sample data
SELECT
    ddr.id,
    ddr.user_id,
    u.name AS user_name,
    ddr.branch_guid,
    ddr.report_date,
    ddr.total_distance_km
FROM daily_distance_reports ddr
LEFT JOIN users u ON u.user_id = ddr.user_id
ORDER BY ddr.report_date DESC
LIMIT 5;
