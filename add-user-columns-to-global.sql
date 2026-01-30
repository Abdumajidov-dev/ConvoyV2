-- =====================================================
-- Migration: Add Worker/Branch columns to users table
-- Global Database: convoydb (10.21.61.51)
-- Purpose: Add PHP API integration fields
-- =====================================================

-- Check if columns exist before adding (safe migration)
DO $$
BEGIN
    -- Add user_id column (PHP API worker_id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='user_id') THEN
        ALTER TABLE users ADD COLUMN user_id INTEGER;
        RAISE NOTICE 'Added column: user_id';
    ELSE
        RAISE NOTICE 'Column already exists: user_id';
    END IF;

    -- Add branch_guid column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='branch_guid') THEN
        ALTER TABLE users ADD COLUMN branch_guid VARCHAR(100);
        RAISE NOTICE 'Added column: branch_guid';
    ELSE
        RAISE NOTICE 'Column already exists: branch_guid';
    END IF;

    -- Add branch_name column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='branch_name') THEN
        ALTER TABLE users ADD COLUMN branch_name VARCHAR(200);
        RAISE NOTICE 'Added column: branch_name';
    ELSE
        RAISE NOTICE 'Column already exists: branch_name';
    END IF;

    -- Add image column (user avatar URL)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='image') THEN
        ALTER TABLE users ADD COLUMN image TEXT;
        RAISE NOTICE 'Added column: image';
    ELSE
        RAISE NOTICE 'Column already exists: image';
    END IF;

    -- Add user_type column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='user_type') THEN
        ALTER TABLE users ADD COLUMN user_type VARCHAR(50);
        RAISE NOTICE 'Added column: user_type';
    ELSE
        RAISE NOTICE 'Column already exists: user_type';
    END IF;

    -- Add role column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='role') THEN
        ALTER TABLE users ADD COLUMN role VARCHAR(100);
        RAISE NOTICE 'Added column: role';
    ELSE
        RAISE NOTICE 'Column already exists: role';
    END IF;

    -- Add worker_guid column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='worker_guid') THEN
        ALTER TABLE users ADD COLUMN worker_guid VARCHAR(100);
        RAISE NOTICE 'Added column: worker_guid';
    ELSE
        RAISE NOTICE 'Column already exists: worker_guid';
    END IF;

    -- Add position_id column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='position_id') THEN
        ALTER TABLE users ADD COLUMN position_id INTEGER;
        RAISE NOTICE 'Added column: position_id';
    ELSE
        RAISE NOTICE 'Column already exists: position_id';
    END IF;
END $$;

-- Create indexes for frequently queried columns
CREATE INDEX IF NOT EXISTS idx_users_user_id ON users(user_id);
CREATE INDEX IF NOT EXISTS idx_users_worker_guid ON users(worker_guid);
CREATE INDEX IF NOT EXISTS idx_users_branch_guid ON users(branch_guid);
CREATE INDEX IF NOT EXISTS idx_users_position_id ON users(position_id);

-- Verify new structure
SELECT
    column_name,
    data_type,
    character_maximum_length,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'users'
ORDER BY ordinal_position;

RAISE NOTICE 'Migration completed successfully!';
