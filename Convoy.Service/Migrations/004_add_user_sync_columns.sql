-- Add columns for PHP API user sync
-- Migration: 004_add_user_sync_columns

-- Add user_id column (PHP API worker_id)
ALTER TABLE users
ADD COLUMN IF NOT EXISTS user_id INTEGER UNIQUE;

-- Add worker_guid column (PHP API UUID)
ALTER TABLE users
ADD COLUMN IF NOT EXISTS worker_guid VARCHAR(100);

-- Add position_id column (PHP API position)
ALTER TABLE users
ADD COLUMN IF NOT EXISTS position_id INTEGER;

-- Create index on user_id for fast lookup
CREATE INDEX IF NOT EXISTS idx_users_user_id ON users(user_id) WHERE user_id IS NOT NULL;

-- Create index on worker_guid for fast lookup
CREATE INDEX IF NOT EXISTS idx_users_worker_guid ON users(worker_guid) WHERE worker_guid IS NOT NULL;

-- Add comment
COMMENT ON COLUMN users.user_id IS 'PHP API worker_id - external system unique identifier';
COMMENT ON COLUMN users.worker_guid IS 'PHP API worker_guid - UUID from external system';
COMMENT ON COLUMN users.position_id IS 'PHP API position_id - worker position/role in external system';
