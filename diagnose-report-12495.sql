-- ============================================
-- DIAGNOSTIKA: Report 12495 ni to'liq tekshirish
-- ============================================

-- 1. Report'ning hozirgi holatini ko'rish
SELECT
    id,
    user_id,
    report_date,
    total_distance_meters,
    total_distance_km,
    location_count,
    first_location_time,
    last_location_time,
    updated_at
FROM daily_distance_reports
WHERE id = 12495;

-- 2. User 17 uchun bugungi locationlar
SELECT
    COUNT(*) as total_locations,
    COUNT(distance_from_previous) as locations_with_distance,
    SUM(distance_from_previous) as sum_distance,
    MIN(recorded_at) as first_time,
    MAX(recorded_at) as last_time
FROM locations
WHERE user_id = 17
  AND DATE(recorded_at) = CURRENT_DATE;

-- 3. Oxirgi 5 ta location (distance bilan)
SELECT
    id,
    user_id,
    recorded_at,
    latitude,
    longitude,
    distance_from_previous,
    is_moving
FROM locations
WHERE user_id = 17
ORDER BY recorded_at DESC
LIMIT 5;

-- 4. Migration 007 apply qilinganmi? (FK constraint tekshirish)
SELECT
    constraint_name,
    table_name,
    column_name,
    foreign_table_name,
    foreign_column_name
FROM (
    SELECT
        tc.constraint_name,
        tc.table_name,
        kcu.column_name,
        ccu.table_name AS foreign_table_name,
        ccu.column_name AS foreign_column_name
    FROM information_schema.table_constraints AS tc
    JOIN information_schema.key_column_usage AS kcu
        ON tc.constraint_name = kcu.constraint_name
    JOIN information_schema.constraint_column_usage AS ccu
        ON ccu.constraint_name = tc.constraint_name
    WHERE tc.constraint_type = 'FOREIGN KEY'
        AND tc.table_name = 'daily_distance_reports'
        AND kcu.column_name = 'user_id'
) AS fk_info;

-- 5. PostgreSQL function versiyasini tekshirish
SELECT
    proname as function_name,
    pg_get_functiondef(oid) as function_definition
FROM pg_proc
WHERE proname = 'calculate_daily_distance'
LIMIT 1;
