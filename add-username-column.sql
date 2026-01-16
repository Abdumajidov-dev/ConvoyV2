-- Migration: Add username column to users table
-- Run this SQL script to update existing database
-- Date: 2026-01-05

-- Step 1: Add username column (nullable)
ALTER TABLE users ADD COLUMN IF NOT EXISTS username VARCHAR(100);

-- Step 2: Create unique index on username
CREATE UNIQUE INDEX IF NOT EXISTS idx_users_username ON users(username) WHERE username IS NOT NULL;

-- Step 3: (Optional) Populate username from phone or generate from name
-- Uncomment if you want to populate existing records
-- UPDATE users SET username = LOWER(REPLACE(name, ' ', '_')) WHERE username IS NULL;

-- Verification: Check column exists
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_name = 'users' AND column_name = 'username';

-- SUCCESS
SELECT 'Username column migration completed!' AS status;
