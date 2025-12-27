-- ================================================
-- Update Locations Table for Flutter Background Geolocation
-- Migration Script - PostgreSQL
-- ================================================

-- IMPORTANT: Bu script mavjud locations table'ni yangilaydi
-- Agar mavjud data bor bo'lsa, backup oling!

-- 1. Yangi column'lar qo'shish
ALTER TABLE locations
  -- Coords related
  ADD COLUMN IF NOT EXISTS ellipsoidal_altitude DECIMAL(10, 6),
  ADD COLUMN IF NOT EXISTS heading_accuracy DECIMAL(10, 6),
  ADD COLUMN IF NOT EXISTS speed_accuracy DECIMAL(10, 6),
  ADD COLUMN IF NOT EXISTS altitude_accuracy DECIMAL(10, 6),
  ADD COLUMN IF NOT EXISTS floor INTEGER,

  -- Battery
  ADD COLUMN IF NOT EXISTS battery_is_charging BOOLEAN DEFAULT false,

  -- Activity (allaqachon bor, lekin rename qilamiz)
  -- activity_type va activity_confidence allaqachon mavjud

  -- Location metadata
  ADD COLUMN IF NOT EXISTS timestamp TIMESTAMPTZ, -- Flutter timestamp
  ADD COLUMN IF NOT EXISTS age DECIMAL(10, 2), -- Location age in milliseconds
  ADD COLUMN IF NOT EXISTS event VARCHAR(50), -- motionchange, heartbeat, providerchange, geofence
  ADD COLUMN IF NOT EXISTS mock BOOLEAN DEFAULT false, -- Android mock location
  ADD COLUMN IF NOT EXISTS sample BOOLEAN DEFAULT false, -- Is this a sample location
  ADD COLUMN IF NOT EXISTS odometer DECIMAL(15, 2), -- Distance traveled
  ADD COLUMN IF NOT EXISTS uuid VARCHAR(100), -- Unique identifier
  ADD COLUMN IF NOT EXISTS extras JSONB; -- Arbitrary extras

-- 2. Eski column'larni yangilash (agar kerak bo'lsa)
-- battery_level allaqachon mavjud (0.0 - 1.0 format)
-- is_moving allaqachon mavjud
-- activity_type allaqachon mavjud
-- activity_confidence allaqachon mavjud

-- 3. Index'lar qo'shish (performance uchun)
CREATE INDEX IF NOT EXISTS idx_locations_uuid ON locations(uuid);
CREATE INDEX IF NOT EXISTS idx_locations_event ON locations(event);
CREATE INDEX IF NOT EXISTS idx_locations_is_moving ON locations(is_moving);
CREATE INDEX IF NOT EXISTS idx_locations_mock ON locations(mock);
CREATE INDEX IF NOT EXISTS idx_locations_timestamp ON locations(timestamp);

-- 4. Comment'lar qo'shish (documentation)
COMMENT ON COLUMN locations.ellipsoidal_altitude IS 'Altitude above WGS84 reference ellipsoid (meters)';
COMMENT ON COLUMN locations.heading_accuracy IS 'Heading accuracy in degrees';
COMMENT ON COLUMN locations.speed_accuracy IS 'Speed accuracy in meters/second';
COMMENT ON COLUMN locations.altitude_accuracy IS 'Altitude accuracy in meters';
COMMENT ON COLUMN locations.floor IS 'Floor within a building (iOS only)';
COMMENT ON COLUMN locations.battery_is_charging IS 'Is device plugged in to power';
COMMENT ON COLUMN locations.timestamp IS 'Flutter timestamp (ISO 8601 UTC format)';
COMMENT ON COLUMN locations.age IS 'Age of location in milliseconds';
COMMENT ON COLUMN locations.event IS 'Event that caused this location: motionchange, heartbeat, providerchange, geofence';
COMMENT ON COLUMN locations.mock IS 'Android only - true if location from mock app';
COMMENT ON COLUMN locations.sample IS 'True if this is sample location (ignore for upload)';
COMMENT ON COLUMN locations.odometer IS 'Current distance traveled in meters';
COMMENT ON COLUMN locations.uuid IS 'Universally Unique Identifier';
COMMENT ON COLUMN locations.extras IS 'Arbitrary extras object (JSON)';

-- 5. Verify yangi structure
SELECT
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'locations'
  AND table_schema = 'public'
ORDER BY ordinal_position;

-- 6. Barcha partition'larga ham qo'llash kerak!
-- Partitioned table bo'lgani uchun, yangi column'lar avtomatik barcha partition'larga qo'shiladi

-- Verification query - partition'larni ko'rish
SELECT
    tablename as partition_name,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables
WHERE tablename LIKE 'locations_%'
ORDER BY tablename DESC
LIMIT 5;

-- ================================================
-- Migration Summary
-- ================================================

/*
YANGI COLUMN'LAR:
1. ellipsoidal_altitude - Altitude above WGS84 ellipsoid
2. heading_accuracy - Heading accuracy
3. speed_accuracy - Speed accuracy
4. altitude_accuracy - Altitude accuracy
5. floor - Building floor (iOS)
6. battery_is_charging - Battery charging status
7. timestamp - Flutter timestamp
8. age - Location age (ms)
9. event - Event type
10. mock - Mock location flag
11. sample - Sample location flag
12. odometer - Distance traveled
13. uuid - Unique ID
14. extras - JSON extras

MAVJUD COLUMN'LAR (o'zgarmaydi):
- id, user_id, recorded_at
- latitude, longitude, accuracy
- altitude, heading, speed
- activity_type, activity_confidence
- is_moving, battery_level, is_charging
- distance_from_previous
- created_at

YANGI INDEX'LAR:
- uuid, event, is_moving, mock, timestamp
*/

COMMIT;
