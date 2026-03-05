-- ============================================
-- DIAGNOSTIKA: User 5475 uchun 2026-03-02 sanada distance 0 sababi
-- ============================================

-- 1. User mavjudligini tekshirish
SELECT
    id as internal_id,
    user_id as external_id,
    name,
    is_active
FROM users
WHERE user_id = 5475;

-- 2. Kechagi locationlar soni
SELECT
    COUNT(*) as total_locations,
    COUNT(CASE WHEN distance_from_previous IS NOT NULL AND distance_from_previous > 0 THEN 1 END) as with_distance,
    MIN(recorded_at) as first_location,
    MAX(recorded_at) as last_location,
    SUM(distance_from_previous) as total_distance_meters
FROM locations
WHERE user_id = 5475
  AND DATE(recorded_at) = '2026-03-02';

-- 3. Bir nechta location sample ko'rish
SELECT
    id,
    user_id,
    recorded_at,
    latitude,
    longitude,
    distance_from_previous,
    is_moving
FROM locations
WHERE user_id = 5475
  AND DATE(recorded_at) = '2026-03-02'
ORDER BY recorded_at ASC
LIMIT 10;

-- 4. PostgreSQL function test qilish
SELECT * FROM calculate_daily_distance(5475, '2026-03-02'::DATE);

-- 5. Daily report'ni tekshirish
SELECT
    id,
    user_id,
    report_date,
    total_distance_meters,
    total_distance_km,
    location_count,
    first_location_time,
    last_location_time
FROM daily_distance_reports
WHERE user_id = 5475
  AND report_date = '2026-03-02';

-- 6. Trigger ishlab turibdimi tekshirish
SELECT
    tgname as trigger_name,
    tgenabled as enabled,
    tgtype as trigger_type
FROM pg_trigger
WHERE tgname = 'trg_update_daily_distance';
