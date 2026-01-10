-- Add branch_guid column to users table
-- Author: Claude Code
-- Date: 2026-01-06

-- Add column
ALTER TABLE users
ADD COLUMN IF NOT EXISTS branch_guid VARCHAR(255);

-- Add index for faster lookups (optional but recommended)
CREATE INDEX IF NOT EXISTS idx_users_branch_guid ON users(branch_guid);

-- Add comment
COMMENT ON COLUMN users.branch_guid IS 'PHP API dan keluvchi branch GUID (nullable)';

-- Check column added successfully
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'users'
  AND column_name = 'branch_guid';
