-- ============================================
-- DIAGNOSTIC: Kunlik masofa 0 kelishini tekshirish
-- ============================================

-- 1. User 5277 bugungi location'lari bormi?
SELECT
    COUNT(*) as total_locations,
    DATE(recorded_at) as location_date,
    MIN(recorded_at) as first_location,
    MAX(recorded_at) as last_location
FROM locations
WHERE user_id = 5277
  AND DATE(recorded_at) = CURRENT_DATE
GROUP BY DATE(recorded_at);

-- 2. User 5277 kechagi location'lari bormi?
SELECT
    COUNT(*) as total_locations,
    DATE(recorded_at) as location_date,
    MIN(recorded_at) as first_location,
    MAX(recorded_at) as last_location
FROM locations
WHERE user_id = 5277
  AND DATE(recorded_at) = CURRENT_DATE - INTERVAL '1 day'
GROUP BY DATE(recorded_at);

-- 3. User 5277 uchun distance_from_previous bor-yo'qligi?
SELECT
    COUNT(*) as total_locations,
    COUNT(distance_from_previous) as with_distance,
    COUNT(*) - COUNT(distance_from_previous) as without_distance,
    AVG(distance_from_previous) as avg_distance,
    SUM(distance_from_previous) as total_distance_meters
FROM locations
WHERE user_id = 5277
  AND DATE(recorded_at) = CURRENT_DATE;

-- 4. Kecha uchun ham tekshirish
SELECT
    COUNT(*) as total_locations,
    COUNT(distance_from_previous) as with_distance,
    COUNT(*) - COUNT(distance_from_previous) as without_distance,
    AVG(distance_from_previous) as avg_distance,
    SUM(distance_from_previous) as total_distance_meters
FROM locations
WHERE user_id = 5277
  AND DATE(recorded_at) = CURRENT_DATE - INTERVAL '1 day';

-- 5. users.user_id va locations.user_id mos kelishini tekshirish
SELECT
    u.id as internal_id,
    u.user_id as external_id,
    u.name,
    u.phone
FROM users u
WHERE u.user_id = 5277;

-- 6. PostgreSQL function manual test qilish (BUGUN)
SELECT * FROM upsert_daily_distance_report(5277, CURRENT_DATE);

-- 7. Yaratilgan hisobotni ko'rish
SELECT
    ddr.id,
    ddr.user_id,
    ddr.report_date,
    ddr.total_distance_meters,
    ddr.total_distance_km,
    ddr.location_count,
    ddr.first_location_time,
    ddr.last_location_time,
    u.name as user_name
FROM daily_distance_reports ddr
LEFT JOIN users u ON u.user_id = ddr.user_id
WHERE ddr.user_id = 5277
  AND ddr.report_date = CURRENT_DATE
ORDER BY ddr.created_at DESC
LIMIT 1;

-- 8. KECHA uchun PostgreSQL function manual test qilish
SELECT * FROM upsert_daily_distance_report(5277, CURRENT_DATE - INTERVAL '1 day');

-- 9. Kechagi hisobotni ko'rish
SELECT
    ddr.id,
    ddr.user_id,
    ddr.report_date,
    ddr.total_distance_meters,
    ddr.total_distance_km,
    ddr.location_count,
    ddr.first_location_time,
    ddr.last_location_time,
    u.name as user_name
FROM daily_distance_reports ddr
LEFT JOIN users u ON u.user_id = ddr.user_id
WHERE ddr.user_id = 5277
  AND ddr.report_date = CURRENT_DATE - INTERVAL '1 day'
ORDER BY ddr.created_at DESC
LIMIT 1;

-- 10. Barcha location'lar uchun distance_from_previous statistika
SELECT
    DATE(recorded_at) as date,
    user_id,
    COUNT(*) as total_locations,
    COUNT(distance_from_previous) as with_distance,
    SUM(distance_from_previous) as total_distance_meters,
    ROUND(SUM(distance_from_previous) / 1000, 2) as total_distance_km
FROM locations
WHERE user_id IN (5277, 5475)
  AND recorded_at >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY DATE(recorded_at), user_id
ORDER BY date DESC, user_id;

-- 11. Function definition'ni tekshirish
SELECT
    proname as function_name,
    pg_get_functiondef(oid) as function_definition
FROM pg_proc
WHERE proname = 'upsert_daily_distance_report';
