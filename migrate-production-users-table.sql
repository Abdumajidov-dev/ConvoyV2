-- Production database migration: Add missing columns to users table
-- Run this on Railway production database

-- Check current columns
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_name = 'users'
ORDER BY ordinal_position;

-- Add missing columns
ALTER TABLE users ADD COLUMN IF NOT EXISTS user_id INTEGER;
ALTER TABLE users ADD COLUMN IF NOT EXISTS branch_guid VARCHAR(100);
ALTER TABLE users ADD COLUMN IF NOT EXISTS branch_name VARCHAR(200);
ALTER TABLE users ADD COLUMN IF NOT EXISTS worker_guid VARCHAR(100);
ALTER TABLE users ADD COLUMN IF NOT EXISTS position_id INTEGER;
ALTER TABLE users ADD COLUMN IF NOT EXISTS image VARCHAR(500);
ALTER TABLE users ADD COLUMN IF NOT EXISTS user_type VARCHAR(50);
ALTER TABLE users ADD COLUMN IF NOT EXISTS role VARCHAR(100);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_users_user_id ON users(user_id) WHERE user_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_users_role ON users(role) WHERE role IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_users_phone ON users(phone) WHERE phone IS NOT NULL;

-- Verify columns added
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_name = 'users'
ORDER BY ordinal_position;

-- Done!
SELECT 'Migration completed successfully!' AS status;
