-- ============================================
-- MIGRATION: Change daily_distance_reports.user_id to EXTERNAL ID
-- ============================================
-- BEFORE: user_id → users.id (internal DB primary key)
-- AFTER:  user_id → users.user_id (external PHP worker_id)

-- Step 1: Drop existing FK constraint
ALTER TABLE daily_distance_reports
DROP CONSTRAINT IF EXISTS daily_distance_reports_user_id_fkey;

-- Step 2: Drop old UNIQUE constraint (if exists)
ALTER TABLE daily_distance_reports
DROP CONSTRAINT IF EXISTS daily_distance_reports_user_id_report_date_key;

-- Step 3: Update existing data - convert internal ID to external ID
UPDATE daily_distance_reports ddr
SET user_id = u.user_id
FROM users u
WHERE ddr.user_id = u.id
  AND u.user_id IS NOT NULL;

-- Step 4: Create new FK constraint to users.user_id (UNIQUE column)
ALTER TABLE daily_distance_reports
ADD CONSTRAINT daily_distance_reports_user_id_fkey
FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE;

-- Step 5: Re-create UNIQUE constraint
ALTER TABLE daily_distance_reports
ADD CONSTRAINT daily_distance_reports_user_id_report_date_key
UNIQUE (user_id, report_date);

-- Step 6: Drop and recreate indexes
DROP INDEX IF EXISTS idx_daily_distance_user_date;
DROP INDEX IF EXISTS idx_daily_distance_date;

CREATE INDEX idx_daily_distance_user_date ON daily_distance_reports(user_id, report_date DESC);
CREATE INDEX idx_daily_distance_date ON daily_distance_reports(report_date DESC);

-- ============================================
-- FUNCTION: Kunlik masofani hisoblash (UPDATED - uses EXTERNAL ID)
-- p_user_id = users.user_id (external PHP worker_id)
-- ============================================
DROP FUNCTION IF EXISTS calculate_daily_distance(INTEGER, DATE);
CREATE OR REPLACE FUNCTION calculate_daily_distance(p_user_id INTEGER, p_date DATE)
RETURNS TABLE(
    total_distance_meters DECIMAL(12, 2),
    total_distance_km DECIMAL(10, 2),
    location_count INTEGER,
    first_location_time TIMESTAMPTZ,
    last_location_time TIMESTAMPTZ
) AS $$
BEGIN
    -- DIRECT: p_user_id allaqachon external ID (users.user_id)
    -- locations.user_id ham external ID
    RETURN QUERY
    SELECT
        COALESCE(SUM(l.distance_from_previous), 0)::DECIMAL(12, 2),
        COALESCE(ROUND(SUM(l.distance_from_previous) / 1000, 2), 0)::DECIMAL(10, 2),
        COUNT(CASE WHEN l.distance_from_previous IS NOT NULL THEN 1 END)::INTEGER,
        MIN(l.recorded_at),
        MAX(l.recorded_at)
    FROM locations l
    WHERE l.user_id = p_user_id
      AND DATE(l.recorded_at) = p_date;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- FUNCTION: Kunlik hisobotni upsert qilish (UPDATED - uses EXTERNAL ID)
-- p_user_id = users.user_id (external PHP worker_id)
-- ============================================
DROP FUNCTION IF EXISTS upsert_daily_distance_report(INTEGER, DATE);
CREATE OR REPLACE FUNCTION upsert_daily_distance_report(p_user_id INTEGER, p_date DATE)
RETURNS BIGINT AS $$
DECLARE
    v_report_id BIGINT;
    v_data RECORD;
BEGIN
    -- DIRECT: p_user_id allaqachon external ID (users.user_id)
    SELECT * INTO v_data FROM calculate_daily_distance(p_user_id, p_date);

    INSERT INTO daily_distance_reports (
        user_id, report_date,
        total_distance_meters, total_distance_km, location_count,
        first_location_time, last_location_time,
        created_at, updated_at
    )
    VALUES (
        p_user_id, p_date,
        v_data.total_distance_meters, v_data.total_distance_km, v_data.location_count,
        v_data.first_location_time, v_data.last_location_time,
        NOW(), NOW()
    )
    ON CONFLICT (user_id, report_date) DO UPDATE SET
        total_distance_meters = v_data.total_distance_meters,
        total_distance_km = v_data.total_distance_km,
        location_count = v_data.location_count,
        first_location_time = v_data.first_location_time,
        last_location_time = v_data.last_location_time,
        updated_at = NOW()
    RETURNING id INTO v_report_id;

    RETURN v_report_id;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- FUNCTION: Bir sana uchun barcha userlar hisobotini yaratish (UPDATED)
-- Returns external user_id instead of internal
-- ============================================
DROP FUNCTION IF EXISTS generate_daily_reports_for_date(DATE);
CREATE OR REPLACE FUNCTION generate_daily_reports_for_date(p_date DATE)
RETURNS TABLE(user_id INTEGER, report_id BIGINT, distance_km DECIMAL(10, 2)) AS $$
DECLARE
    v_user RECORD;
    v_report_id BIGINT;
BEGIN
    FOR v_user IN
        SELECT DISTINCT u.user_id AS external_id
        FROM locations l
        JOIN users u ON l.user_id = u.user_id  -- External ID orqali join
        WHERE DATE(l.recorded_at) = p_date
          AND u.user_id IS NOT NULL
    LOOP
        v_report_id := upsert_daily_distance_report(v_user.external_id, p_date);
        IF v_report_id IS NOT NULL THEN
            user_id := v_user.external_id;  -- External ID qaytarish
            report_id := v_report_id;
            SELECT ddr.total_distance_km INTO distance_km
            FROM daily_distance_reports ddr WHERE ddr.id = v_report_id;
            RETURN NEXT;
        END IF;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- TRIGGER: Yangi location qo'shilganda avtomatik yangilash (UPDATED)
-- ============================================
DROP FUNCTION IF EXISTS update_daily_distance_on_location_insert() CASCADE;
CREATE OR REPLACE FUNCTION update_daily_distance_on_location_insert()
RETURNS TRIGGER AS $$
BEGIN
    -- DIRECT: NEW.user_id allaqachon external ID (locations.user_id)
    -- Check if user exists in users table
    IF EXISTS (SELECT 1 FROM users u WHERE u.user_id = NEW.user_id) THEN
        PERFORM upsert_daily_distance_report(NEW.user_id, DATE(NEW.recorded_at));
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_update_daily_distance ON locations;
CREATE TRIGGER trg_update_daily_distance
    AFTER INSERT ON locations
    FOR EACH ROW
    EXECUTE FUNCTION update_daily_distance_on_location_insert();

-- ============================================
-- VERIFICATION QUERIES
-- ============================================
-- Check FK constraint
SELECT
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
    AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
    AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
    AND tc.table_name='daily_distance_reports';

-- Check sample data
SELECT
    ddr.id,
    ddr.user_id as external_user_id,
    u.id as internal_user_id,
    u.name,
    ddr.report_date,
    ddr.total_distance_km
FROM daily_distance_reports ddr
LEFT JOIN users u ON ddr.user_id = u.user_id
ORDER BY ddr.id DESC
LIMIT 5;
