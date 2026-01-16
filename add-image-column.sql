-- Add image column to users table
-- Date: 2026-01-07

-- Check if column exists before adding
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'users'
        AND column_name = 'image'
    ) THEN
        ALTER TABLE users ADD COLUMN image TEXT;
        RAISE NOTICE 'Column "image" added to users table';
    ELSE
        RAISE NOTICE 'Column "image" already exists in users table';
    END IF;
END $$;

-- Verify the column was added
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'users'
ORDER BY ordinal_position;
