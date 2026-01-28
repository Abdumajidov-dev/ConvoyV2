-- Add missing columns to users table
-- Run this script to update your database schema

-- Add branch_name column (PHP API filial_name)
ALTER TABLE users ADD COLUMN IF NOT EXISTS branch_name VARCHAR(200);

-- Add user_type column (PHP API user type: "worker", "admin", etc.)
ALTER TABLE users ADD COLUMN IF NOT EXISTS user_type VARCHAR(50);

-- Add role column (PHP API user role: "operator_admin_chat", "driver", etc.)
ALTER TABLE users ADD COLUMN IF NOT EXISTS role VARCHAR(100);

-- Create index for role lookups
CREATE INDEX IF NOT EXISTS idx_users_role ON users(role) WHERE role IS NOT NULL;

-- Verify changes
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_name = 'users'
ORDER BY ordinal_position;
