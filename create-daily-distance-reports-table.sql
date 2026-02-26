-- ============================================
-- DAILY DISTANCE REPORTS TABLE
-- ============================================
-- Har bir foydalanuvchi kunlik bosib o'tgan masofani saqlash uchun
-- Bu jadval har kuni avtomatik yangilanadi va hisobot uchun ishlatiladi

CREATE TABLE IF NOT EXISTS daily_distance_reports (
    id BIGSERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL,
    report_date DATE NOT NULL,
    total_distance_meters DECIMAL(12, 2) NOT NULL DEFAULT 0,
    total_distance_km DECIMAL(10, 2) NOT NULL DEFAULT 0,
    location_count INTEGER NOT NULL DEFAULT 0,
    first_location_time TIMESTAMPTZ,
    last_location_time TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),

    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    UNIQUE (user_id, report_date)
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_daily_distance_user_date ON daily_distance_reports(user_id, report_date DESC);
CREATE INDEX IF NOT EXISTS idx_daily_distance_date ON daily_distance_reports(report_date DESC);
CREATE INDEX IF NOT EXISTS idx_daily_distance_user ON daily_distance_reports(user_id);

-- ============================================
-- FUNCTION: Kunlik masofani hisoblash
-- ============================================
-- Bu funksiya berilgan sana uchun foydalanuvchining umumiy masofasini hisoblaydi
CREATE OR REPLACE FUNCTION calculate_daily_distance(
    p_user_id INTEGER,
    p_date DATE
)
RETURNS TABLE(
    total_distance_meters DECIMAL(12, 2),
    total_distance_km DECIMAL(10, 2),
    location_count INTEGER,
    first_location_time TIMESTAMPTZ,
    last_location_time TIMESTAMPTZ
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        COALESCE(SUM(distance_from_previous), 0)::DECIMAL(12, 2) as total_distance_meters,
        COALESCE(ROUND(SUM(distance_from_previous) / 1000, 2), 0)::DECIMAL(10, 2) as total_distance_km,
        COUNT(*)::INTEGER as location_count,
        MIN(recorded_at) as first_location_time,
        MAX(recorded_at) as last_location_time
    FROM locations
    WHERE user_id = p_user_id
        AND DATE(recorded_at) = p_date
        AND distance_from_previous IS NOT NULL;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- FUNCTION: Kunlik hisobotni yangilash yoki yaratish
-- ============================================
-- Bu funksiya mavjud hisobotni yangilaydi yoki yangi hisobotni yaratadi
CREATE OR REPLACE FUNCTION upsert_daily_distance_report(
    p_user_id INTEGER,
    p_date DATE
)
RETURNS BIGINT AS $$
DECLARE
    v_report_id BIGINT;
    v_distance_data RECORD;
BEGIN
    -- Masofani hisoblash
    SELECT * INTO v_distance_data
    FROM calculate_daily_distance(p_user_id, p_date);

    -- Mavjud hisobotni yangilash yoki yangi yaratish
    INSERT INTO daily_distance_reports (
        user_id,
        report_date,
        total_distance_meters,
        total_distance_km,
        location_count,
        first_location_time,
        last_location_time,
        updated_at
    )
    VALUES (
        p_user_id,
        p_date,
        v_distance_data.total_distance_meters,
        v_distance_data.total_distance_km,
        v_distance_data.location_count,
        v_distance_data.first_location_time,
        v_distance_data.last_location_time,
        NOW()
    )
    ON CONFLICT (user_id, report_date)
    DO UPDATE SET
        total_distance_meters = v_distance_data.total_distance_meters,
        total_distance_km = v_distance_data.total_distance_km,
        location_count = v_distance_data.location_count,
        first_location_time = v_distance_data.first_location_time,
        last_location_time = v_distance_data.last_location_time,
        updated_at = NOW()
    RETURNING id INTO v_report_id;

    RETURN v_report_id;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- FUNCTION: Ko'p foydalanuvchilar uchun hisobotni yaratish
-- ============================================
-- Bu funksiya barcha aktiv foydalanuvchilar uchun kunlik hisobotni yaratadi
CREATE OR REPLACE FUNCTION generate_daily_reports_for_date(p_date DATE)
RETURNS TABLE(user_id INTEGER, report_id BIGINT, distance_km DECIMAL(10, 2)) AS $$
DECLARE
    v_user_record RECORD;
    v_report_id BIGINT;
BEGIN
    -- Barcha aktiv foydalanuvchilar uchun hisobot yaratish
    FOR v_user_record IN
        SELECT DISTINCT l.user_id
        FROM locations l
        WHERE DATE(l.recorded_at) = p_date
    LOOP
        v_report_id := upsert_daily_distance_report(v_user_record.user_id, p_date);

        RETURN QUERY
        SELECT
            v_user_record.user_id,
            v_report_id,
            ddr.total_distance_km
        FROM daily_distance_reports ddr
        WHERE ddr.id = v_report_id;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- TRIGGER: Location qo'shilganda avtomatik hisobotni yangilash
-- ============================================
CREATE OR REPLACE FUNCTION update_daily_distance_on_location_insert()
RETURNS TRIGGER AS $$
BEGIN
    -- Yangi location qo'shilganda shu kun uchun hisobotni yangilash
    PERFORM upsert_daily_distance_report(
        NEW.user_id,
        DATE(NEW.recorded_at)
    );

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggerni locations jadvaliga qo'shish
DROP TRIGGER IF EXISTS trg_update_daily_distance ON locations;
CREATE TRIGGER trg_update_daily_distance
    AFTER INSERT ON locations
    FOR EACH ROW
    EXECUTE FUNCTION update_daily_distance_on_location_insert();

-- ============================================
-- TEST QUERIES (Qo'llanma)
-- ============================================

-- 1. Bitta foydalanuvchi uchun bugungi kunlik hisobotni yaratish
-- SELECT upsert_daily_distance_report(5277, CURRENT_DATE);

-- 2. Barcha foydalanuvchilar uchun bugungi hisobotni yaratish
-- SELECT * FROM generate_daily_reports_for_date(CURRENT_DATE);

-- 3. Barcha foydalanuvchilar uchun kechagi hisobotni yaratish
-- SELECT * FROM generate_daily_reports_for_date(CURRENT_DATE - INTERVAL '1 day');

-- 4. Ma'lum sana oralig'idagi hisobotlarni ko'rish
-- SELECT
--     u.name,
--     u.phone,
--     ddr.report_date,
--     ddr.total_distance_km,
--     ddr.location_count,
--     ddr.first_location_time,
--     ddr.last_location_time
-- FROM daily_distance_reports ddr
-- JOIN users u ON ddr.user_id = u.id
-- WHERE ddr.report_date >= '2026-02-01'
--   AND ddr.report_date <= '2026-02-28'
-- ORDER BY ddr.report_date DESC, ddr.total_distance_km DESC;

-- 5. Eng ko'p masofa bosgan foydalanuvchilar (bugungi kun)
-- SELECT
--     u.name,
--     u.phone,
--     ddr.total_distance_km,
--     ddr.location_count
-- FROM daily_distance_reports ddr
-- JOIN users u ON ddr.user_id = u.id
-- WHERE ddr.report_date = CURRENT_DATE
-- ORDER BY ddr.total_distance_km DESC
-- LIMIT 10;
