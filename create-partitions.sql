-- Partition'larni yaratish
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE - INTERVAL '1 month')::DATE) as prev_month;
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE)::DATE) as current_month;
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE + INTERVAL '1 month')::DATE) as next_month;
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE + INTERVAL '2 months')::DATE) as month_2;
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE + INTERVAL '3 months')::DATE) as month_3;

-- Partition'larni ko'rish
SELECT tablename FROM pg_tables WHERE tablename LIKE 'locations_%' ORDER BY tablename;
