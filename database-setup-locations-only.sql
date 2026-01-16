-- Convoy GPS Tracking - Location Partitioned Table Only
-- Agar User table uchun EF Core migrations ishlatsangiz

-- ============================================
-- LOCATIONS PARTITIONED TABLE
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

-- Indexes
CREATE INDEX IF NOT EXISTS idx_locations_user_time ON locations (user_id, recorded_at DESC);
CREATE INDEX IF NOT EXISTS idx_locations_time ON locations (recorded_at DESC);
CREATE INDEX IF NOT EXISTS idx_locations_user_moving ON locations (user_id, is_moving) WHERE is_moving = true;

-- ============================================
-- PARTITION YARATISH FUNCTION
-- ============================================
CREATE OR REPLACE FUNCTION create_location_partition(target_month DATE)
RETURNS TEXT AS $$
DECLARE
    partition_name TEXT;
    start_date DATE;
    end_date DATE;
BEGIN
    partition_name := 'locations_' || TO_CHAR(target_month, 'MM_YYYY');
    start_date := DATE_TRUNC('month', target_month);
    end_date := start_date + INTERVAL '1 month';

    IF EXISTS (SELECT 1 FROM pg_tables WHERE tablename = partition_name AND schemaname = 'public') THEN
        RETURN 'Already exists: ' || partition_name;
    END IF;

    EXECUTE format(
        'CREATE TABLE %I PARTITION OF locations FOR VALUES FROM (%L) TO (%L)',
        partition_name, start_date, end_date
    );

    RETURN 'Created: ' || partition_name;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- INITIAL PARTITIONS
-- ============================================
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE - INTERVAL '1 month'));
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE));
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE + INTERVAL '1 month'));
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE + INTERVAL '2 months'));
SELECT create_location_partition(DATE_TRUNC('month', CURRENT_DATE + INTERVAL '3 months'));

-- ============================================
-- HAVERSINE DISTANCE FUNCTION
-- ============================================
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

    RETURN distance_km * 1000;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

SELECT 'Location partitioned table setup completed!' AS status;
