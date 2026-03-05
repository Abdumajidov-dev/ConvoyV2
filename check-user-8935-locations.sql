-- User 8935 oxirgi 5 ta locationini ko'rish
SELECT
    id,
    user_id,
    recorded_at,
    latitude,
    longitude,
    distance_from_previous,
    created_at
FROM locations
WHERE user_id = 8935
ORDER BY recorded_at DESC
LIMIT 5;

-- Distance calculation test (manual)
-- Agar ikki location orasida masofa bo'lsa, bu function qaytarishi kerak
SELECT
    l1.id as loc1_id,
    l1.latitude as lat1,
    l1.longitude as lon1,
    l2.id as loc2_id,
    l2.latitude as lat2,
    l2.longitude as lon2,
    calculate_distance(
        l1.latitude::double precision,
        l1.longitude::double precision,
        l2.latitude::double precision,
        l2.longitude::double precision
    ) as calculated_distance_meters
FROM locations l1
CROSS JOIN locations l2
WHERE l1.user_id = 8935
  AND l2.user_id = 8935
  AND l1.id < l2.id
ORDER BY l1.recorded_at DESC
LIMIT 1;
