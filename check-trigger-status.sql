-- ============================================
-- Trigger va Migration Status Tekshirish
-- ============================================

-- 1. Trigger mavjudmi va enabled'mi?
SELECT
    tgname as trigger_name,
    tgenabled as enabled,
    tgrelid::regclass as table_name,
    pg_get_triggerdef(oid) as trigger_definition
FROM pg_trigger
WHERE tgname = 'trg_update_daily_distance'
  AND tgrelid = 'locations'::regclass;

-- 2. FK constraint qaysi column'ga? (users.id yoki users.user_id?)
SELECT
    tc.constraint_name,
    kcu.column_name,
    ccu.table_name AS foreign_table,
    ccu.column_name AS foreign_column
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
    AND tc.table_name = 'daily_distance_reports'
    AND kcu.column_name = 'user_id';

-- 3. calculate_daily_distance function definition
SELECT pg_get_functiondef(oid) as function_code
FROM pg_proc
WHERE proname = 'calculate_daily_distance'
LIMIT 1;

-- 4. User 8935 uchun bugungi locationlar
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
  AND DATE(recorded_at) = CURRENT_DATE
ORDER BY recorded_at DESC
LIMIT 10;

-- 5. User 8935 uchun daily report
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
WHERE user_id = 8935
  AND report_date = CURRENT_DATE;
