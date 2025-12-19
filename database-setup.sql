-- Convoy GPS Tracking System - Database Setup Script
-- PostgreSQL 12+

-- ============================================
-- 1. DATABASE YARATISH
-- ============================================
-- Agar database mavjud bo'lmasa, yaratish kerak:
-- CREATE DATABASE convoy_db;
-- \c convoy_db

-- ============================================
-- 2. USERS TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    phone VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    delete_at TIMESTAMPTZ
);

-- Users index
CREATE INDEX IF NOT EXISTS idx_users_phone ON users(phone);
CREATE INDEX IF NOT EXISTS idx_users_active ON users(is_active) WHERE is_active = true;

-- ============================================
-- 3. LOCATIONS PARTITIONED TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS locations (
    id BIGSERIAL,
    user_id INTEGER NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL,
    latitude DECIMAL(10, 8) NOT NULL,
    longitude DECIMAL(11, 8) NOT NULL,
    accuracy DECIMAL(6, 2),
    speed DECIMAL(6, 2),
    heading DECIMAL(5, 2),
    altitude DECIMAL(8, 2),
    activity_type VARCHAR(20),
    activity_confidence INTEGER CHECK (activity_confidence >= 0 AND activity_confidence <= 100),
    is_moving BOOLEAN DEFAULT false,
    battery_level INTEGER CHECK (battery_level >= 0 AND battery_level <= 100),
    is_charging BOOLEAN,
    distance_from_previous DECIMAL(10, 2),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    PRIMARY KEY (id, recorded_at),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) PARTITION BY RANGE (recorded_at);

-- Locations indexes
CREATE INDEX IF NOT EXISTS idx_locations_user_time ON locations (user_id, recorded_at DESC);
CREATE INDEX IF NOT EXISTS idx_locations_time ON locations (recorded_at DESC);
CREATE INDEX IF NOT EXISTS idx_locations_user_moving ON locations (user_id, is_moving) WHERE is_moving = true;

-- ============================================
-- 4. PARTITION YARATISH FUNCTION
-- ============================================
CREATE OR REPLACE FUNCTION create_location_partition(target_month DATE)
RETURNS TEXT AS $$
DECLARE
    partition_name TEXT;
    start_date DATE;
    end_date DATE;
BEGIN
    -- Partition nomi: locations_12_2025
    partition_name := 'locations_' || TO_CHAR(target_month, 'MM_YYYY');
    start_date := DATE_TRUNC('month', target_month);
    end_date := start_date + INTERVAL '1 month';

    -- Agar mavjud bo'lsa, skip qilish
    IF EXISTS (SELECT 1 FROM pg_tables WHERE tablename = partition_name AND schemaname = 'public') THEN
        RETURN 'Already exists: ' || partition_name;
    END IF;

    -- Partition yaratish
    EXECUTE format(
        'CREATE TABLE %I PARTITION OF locations FOR VALUES FROM (%L) TO (%L)',
        partition_name, start_date, end_date
    );

    RETURN 'Created: ' || partition_name;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- 5. BOSHLANG'ICH PARTITION'LARNI YARATISH
-- ============================================
-- Hozirgi oy va keyingi 3 oy uchun partition'lar
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE - INTERVAL '1 month')::DATE);
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE)::DATE);
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE + INTERVAL '1 month')::DATE);
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE + INTERVAL '2 months')::DATE);
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE + INTERVAL '3 months')::DATE);

-- ============================================
-- 6. HELPER FUNCTIONS
-- ============================================

-- Haversine distance function (PostgreSQL)
CREATE OR REPLACE FUNCTION calculate_distance(
    lat1 DECIMAL,
    lon1 DECIMAL,
    lat2 DECIMAL,
    lon2 DECIMAL
) RETURNS DECIMAL AS $$
DECLARE
    earth_radius_km CONSTANT DECIMAL := 6371.0;
    dlat DECIMAL;
    dlon DECIMAL;
    a DECIMAL;
    c DECIMAL;
    distance_km DECIMAL;
BEGIN
    dlat := RADIANS(lat2 - lat1);
    dlon := RADIANS(lon2 - lon1);

    a := SIN(dlat / 2) * SIN(dlat / 2) +
         COS(RADIANS(lat1)) * COS(RADIANS(lat2)) *
         SIN(dlon / 2) * SIN(dlon / 2);

    c := 2 * ATAN2(SQRT(a), SQRT(1 - a));

    distance_km := earth_radius_km * c;

    RETURN distance_km * 1000; -- metrga o'girish
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- ============================================
-- 7. USEFUL VIEWS
-- ============================================

-- User statistikalari view
CREATE OR REPLACE VIEW user_location_stats AS
SELECT
    u.id AS user_id,
    u.name,
    COUNT(l.id) AS total_locations,
    MAX(l.recorded_at) AS last_location_time,
    SUM(l.distance_from_previous) AS total_distance_meters,
    ROUND(SUM(l.distance_from_previous) / 1000, 2) AS total_distance_km
FROM users u
LEFT JOIN locations l ON u.id = l.user_id
GROUP BY u.id, u.name;

-- ============================================
-- 8. TEST DATA
-- ============================================

-- Test user yaratish
INSERT INTO users (name, phone, is_active)
VALUES
    ('Test User 1', '+998901234567', true),
    ('Test User 2', '+998907654321', true)
ON CONFLICT DO NOTHING;

-- ============================================
-- 9. PARTITION MAINTENANCE FUNCTION
-- ============================================

-- Eski partition'larni o'chirish (1 yildan eski)
CREATE OR REPLACE FUNCTION drop_old_partitions(months_to_keep INTEGER DEFAULT 12)
RETURNS TEXT AS $$
DECLARE
    partition_record RECORD;
    dropped_count INTEGER := 0;
BEGIN
    FOR partition_record IN
        SELECT tablename
        FROM pg_tables
        WHERE schemaname = 'public'
          AND tablename LIKE 'locations_%'
    LOOP
        -- Partition yaratilgan sanani extract qilish
        -- Format: locations_MM_YYYY
        DECLARE
            partition_month TEXT;
            partition_date DATE;
            cutoff_date DATE;
        BEGIN
            partition_month := SUBSTRING(partition_record.tablename FROM 11);
            partition_date := TO_DATE(partition_month, 'MM_YYYY');
            cutoff_date := DATE_TRUNC('month', CURRENT_DATE - (months_to_keep || ' months')::INTERVAL);

            IF partition_date < cutoff_date THEN
                EXECUTE format('DROP TABLE IF EXISTS %I', partition_record.tablename);
                RAISE NOTICE 'Dropped partition: %', partition_record.tablename;
                dropped_count := dropped_count + 1;
            END IF;
        EXCEPTION
            WHEN OTHERS THEN
                RAISE NOTICE 'Error processing partition %: %', partition_record.tablename, SQLERRM;
        END;
    END LOOP;

    RETURN 'Dropped ' || dropped_count || ' old partitions';
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- 10. MONITORING QUERIES
-- ============================================

-- Partition'larni ko'rish
COMMENT ON TABLE locations IS 'GPS tracking locations with monthly partitioning';

-- ============================================
-- SUCCESS MESSAGE
-- ============================================
SELECT 'Database setup completed successfully!' AS status;

-- Partition'lar soni
SELECT COUNT(*) AS partition_count
FROM pg_tables
WHERE tablename LIKE 'locations_%';
