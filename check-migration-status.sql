-- Check if migration 007 was applied
-- If function uses external ID, migration was successful

-- 1. Check FK constraint (should reference users.user_id, not users.id)
SELECT
    tc.constraint_name,
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
    AND kcu.column_name = 'user_id';

-- 2. Check function source code (should NOT have internal->external conversion)
SELECT
    proname as function_name,
    prosrc as source_code
FROM pg_proc
WHERE proname = 'calculate_daily_distance';
