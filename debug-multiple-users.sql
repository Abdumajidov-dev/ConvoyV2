-- Debug script for multiple users endpoint issue

-- 1. Check if users 5277 and 5475 exist in database
SELECT user_id, name, is_active, role
FROM users
WHERE user_id IN (5277, 5475);

-- 2. Count total locations for 2026-01-31 (UTC)
SELECT COUNT(*) as total_locations
FROM locations
WHERE recorded_at >= '2026-01-30 19:00:00+00'  -- 2026-01-31 00:00 Toshkent
  AND recorded_at < '2026-01-31 19:00:00+00';  -- 2026-02-01 00:00 Toshkent

-- 3. Count locations by user for 2026-01-31
SELECT user_id, COUNT(*) as location_count
FROM locations
WHERE recorded_at >= '2026-01-30 19:00:00+00'
  AND recorded_at < '2026-01-31 19:00:00+00'
GROUP BY user_id
ORDER BY user_id;

-- 4. Get all active users (like GetAllActiveUsersAsync does)
SELECT COUNT(*) as total_active_users
FROM users
WHERE is_active = true
  AND (role IS NULL OR role != 'admin_unduruv');

-- 5. Check if users 5277 and 5475 are in active users list
SELECT user_id, name, is_active, role
FROM users
WHERE user_id IN (5277, 5475);

-- 6. Sample locations for user 5277 on 2026-01-31
SELECT id, user_id, recorded_at, latitude, longitude
FROM locations
WHERE user_id = 5277
  AND recorded_at >= '2026-01-30 19:00:00+00'
  AND recorded_at < '2026-01-31 19:00:00+00'
ORDER BY recorded_at
LIMIT 5;

-- 7. Sample locations for user 5475 on 2026-01-31
SELECT id, user_id, recorded_at, latitude, longitude
FROM locations
WHERE user_id = 5475
  AND recorded_at >= '2026-01-30 19:00:00+00'
  AND recorded_at < '2026-01-31 19:00:00+00'
ORDER BY recorded_at
LIMIT 5;
