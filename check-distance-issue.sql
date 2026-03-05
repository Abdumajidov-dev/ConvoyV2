-- ============================================
-- Check why distance is 0
-- ============================================

-- 1. User 5277 ning BUGUN location'lari
SELECT
    id,
    user_id,
    DATE(recorded_at) as date,
    recorded_at,
    latitude,
    longitude,
    distance_from_previous,
    CASE
        WHEN distance_from_previous IS NULL THEN 'NULL - birinchi location'
        WHEN distance_from_previous = 0 THEN '0 - bir xil joyda'
        ELSE 'OK - masofa bor'
    END as distance_status
FROM locations
WHERE user_id = 5277
  AND DATE(recorded_at) = CURRENT_DATE
ORDER BY recorded_at ASC
LIMIT 20;

-- 2. User 5277 ning KECHA location'lari
SELECT
    id,
    user_id,
    DATE(recorded_at) as date,
    recorded_at,
    distance_from_previous,
    CASE
        WHEN distance_from_previous IS NULL THEN 'NULL'
        WHEN distance_from_previous = 0 THEN '0'
        ELSE 'OK'
    END as status
FROM locations
WHERE user_id = 5277
  AND DATE(recorded_at) = CURRENT_DATE - INTERVAL '1 day'
ORDER BY recorded_at ASC
LIMIT 20;

-- 3. User 5277 uchun distance statistika (oxirgi 7 kun)
SELECT
    DATE(recorded_at) as date,
    COUNT(*) as total_locations,
    COUNT(distance_from_previous) as locations_with_distance,
    COUNT(*) - COUNT(distance_from_previous) as locations_without_distance,
    SUM(distance_from_previous) as total_distance_meters,
    ROUND(SUM(distance_from_previous) / 1000, 2) as total_distance_km
FROM locations
WHERE user_id = 5277
  AND recorded_at >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY DATE(recorded_at)
ORDER BY date DESC;

-- 4. Agar 0 bo'lsa - birinchi location'ning distance_from_previous NULL ekanligini ko'rsatadi
SELECT
    MIN(id) as first_location_id,
    MIN(recorded_at) as first_location_time,
    COUNT(*) as total_count,
    COUNT(distance_from_previous) as with_distance
FROM locations
WHERE user_id = 5277
  AND DATE(recorded_at) = CURRENT_DATE;

-- 5. Agar barcha NULL bo'lsa - avvalgi kun location'lar borligini tekshirish
SELECT
    DATE(recorded_at) as date,
    COUNT(*) as location_count,
    MAX(recorded_at) as last_location_time
FROM locations
WHERE user_id = 5277
  AND recorded_at < CURRENT_DATE
GROUP BY DATE(recorded_at)
ORDER BY date DESC
LIMIT 5;

-- 6. PostgreSQL function manual test (debug)
DO $$
DECLARE
    v_total_distance DECIMAL(12, 2);
    v_location_count INTEGER;
BEGIN
    SELECT
        COALESCE(SUM(distance_from_previous), 0),
        COUNT(*)
    INTO v_total_distance, v_location_count
    FROM locations
    WHERE user_id = 5277
      AND recorded_at >= CURRENT_DATE::TIMESTAMPTZ
      AND recorded_at < (CURRENT_DATE + INTERVAL '1 day')::TIMESTAMPTZ;

    RAISE NOTICE 'User 5277 bugun: % locations, total distance: % meters', v_location_count, v_total_distance;
END $$;
