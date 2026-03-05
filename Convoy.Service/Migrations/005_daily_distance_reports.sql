-- ============================================
-- DAILY DISTANCE REPORTS TABLE
-- ============================================
-- Har bir foydalanuvchi kunlik bosib o'tgan masofani saqlash uchun.
-- MUHIM: locations.user_id = users.user_id (external PHP ID)
--        daily_distance_reports.user_id = users.id (internal DB ID)

CREATE TABLE IF NOT EXISTS daily_distance_reports (
    id BIGSERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    report_date DATE NOT NULL,
    total_distance_meters DECIMAL(12, 2) NOT NULL DEFAULT 0,
    total_distance_km DECIMAL(10, 2) NOT NULL DEFAULT 0,
    location_count INTEGER NOT NULL DEFAULT 0,
    first_location_time TIMESTAMPTZ,
    last_location_time TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE (user_id, report_date)
);

CREATE INDEX IF NOT EXISTS idx_daily_distance_user_date ON daily_distance_reports(user_id, report_date DESC);
CREATE INDEX IF NOT EXISTS idx_daily_distance_date ON daily_distance_reports(report_date DESC);

-- ============================================
-- FUNCTION: Kunlik masofani hisoblash
-- p_internal_id = users.id (internal DB primary key)
-- ============================================
DROP FUNCTION IF EXISTS calculate_daily_distance(INTEGER, DATE);
DROP FUNCTION IF EXISTS calculate_daily_distance(BIGINT, DATE);
CREATE OR REPLACE FUNCTION calculate_daily_distance(p_internal_id INTEGER, p_date DATE)
RETURNS TABLE(
    total_distance_meters DECIMAL(12, 2),
    total_distance_km DECIMAL(10, 2),
    location_count INTEGER,
    first_location_time TIMESTAMPTZ,
    last_location_time TIMESTAMPTZ
) AS $$
DECLARE
    v_external_id INTEGER;
BEGIN
    -- Internal users.id dan external users.user_id (PHP worker_id) ni olish
    SELECT u.user_id INTO v_external_id FROM users u WHERE u.id = p_internal_id;

    RETURN QUERY
    SELECT
        COALESCE(SUM(l.distance_from_previous), 0)::DECIMAL(12, 2),
        COALESCE(ROUND(SUM(l.distance_from_previous) / 1000, 2), 0)::DECIMAL(10, 2),
        COUNT(CASE WHEN l.distance_from_previous IS NOT NULL THEN 1 END)::INTEGER,
        MIN(l.recorded_at),
        MAX(l.recorded_at)
    FROM locations l
    WHERE l.user_id = v_external_id
      AND DATE(l.recorded_at) = p_date;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- FUNCTION: Kunlik hisobotni upsert qilish
-- ============================================
DROP FUNCTION IF EXISTS upsert_daily_distance_report(INTEGER, DATE);
DROP FUNCTION IF EXISTS upsert_daily_distance_report(BIGINT, DATE);
CREATE OR REPLACE FUNCTION upsert_daily_distance_report(p_internal_id INTEGER, p_date DATE)
RETURNS BIGINT AS $$
DECLARE
    v_report_id BIGINT;
    v_data RECORD;
BEGIN
    SELECT * INTO v_data FROM calculate_daily_distance(p_internal_id, p_date);

    INSERT INTO daily_distance_reports (
        user_id, report_date,
        total_distance_meters, total_distance_km, location_count,
        first_location_time, last_location_time,
        created_at, updated_at
    )
    VALUES (
        p_internal_id, p_date,
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
-- FUNCTION: Bir sana uchun barcha userlar hisobotini yaratish
-- ============================================
CREATE OR REPLACE FUNCTION generate_daily_reports_for_date(p_date DATE)
RETURNS TABLE(user_id INTEGER, report_id BIGINT, distance_km DECIMAL(10, 2)) AS $$
DECLARE
    v_user RECORD;
    v_report_id BIGINT;
BEGIN
    FOR v_user IN
        SELECT DISTINCT u.id AS internal_id
        FROM locations l
        JOIN users u ON l.user_id = u.user_id  -- External ID orqali join
        WHERE DATE(l.recorded_at) = p_date
    LOOP
        v_report_id := upsert_daily_distance_report(v_user.internal_id, p_date);
        IF v_report_id IS NOT NULL THEN
            user_id := v_user.internal_id;
            report_id := v_report_id;
            SELECT ddr.total_distance_km INTO distance_km
            FROM daily_distance_reports ddr WHERE ddr.id = v_report_id;
            RETURN NEXT;
        END IF;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- TRIGGER: Yangi location qo'shilganda avtomatik yangilash
-- ============================================
CREATE OR REPLACE FUNCTION update_daily_distance_on_location_insert()
RETURNS TRIGGER AS $$
DECLARE
    v_internal_id INTEGER;
BEGIN
    -- External ID (NEW.user_id) dan internal ID ni olish
    SELECT u.id INTO v_internal_id FROM users u WHERE u.user_id = NEW.user_id;
    IF v_internal_id IS NOT NULL THEN
        PERFORM upsert_daily_distance_report(v_internal_id, DATE(NEW.recorded_at));
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_update_daily_distance ON locations;
CREATE TRIGGER trg_update_daily_distance
    AFTER INSERT ON locations
    FOR EACH ROW
    EXECUTE FUNCTION update_daily_distance_on_location_insert();
