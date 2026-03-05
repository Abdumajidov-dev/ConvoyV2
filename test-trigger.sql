-- ============================================
-- TEST: Trigger ishlab turibdimi?
-- ============================================

-- 1. User 17 uchun bugungi locationlar
SELECT
    COUNT(*) as total_locations,
    COUNT(CASE WHEN distance_from_previous IS NOT NULL AND distance_from_previous > 0 THEN 1 END) as with_distance,
    MIN(recorded_at) as first_time,
    MAX(recorded_at) as last_time,
    SUM(distance_from_previous) as total_distance
FROM locations
WHERE user_id = 17
  AND DATE(recorded_at) = CURRENT_DATE;

-- 2. Oxirgi 10 ta location
SELECT
    id,
    user_id,
    recorded_at,
    latitude,
    longitude,
    distance_from_previous
FROM locations
WHERE user_id = 17
ORDER BY recorded_at DESC
LIMIT 10;

-- 3. PostgreSQL function test
SELECT * FROM calculate_daily_distance(17, CURRENT_DATE);

-- 4. Trigger mavjudligini tekshirish
SELECT
    tgname,
    tgenabled,
    pg_get_triggerdef(oid) as trigger_definition
FROM pg_trigger
WHERE tgname = 'trg_update_daily_distance';

-- 5. Manual trigger call (test)
SELECT upsert_daily_distance_report(17, CURRENT_DATE);

-- 6. Daily report'ni tekshirish
SELECT
    id,
    user_id,
    report_date,
    total_distance_meters,
    location_count
FROM daily_distance_reports
WHERE user_id = 17
  AND report_date = CURRENT_DATE;
