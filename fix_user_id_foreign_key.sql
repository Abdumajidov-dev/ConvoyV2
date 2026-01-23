-- ========================================
-- FIX: locations.user_id foreign key
-- ========================================
-- Muammo: locations.user_id -> users.id (wrong)
-- Yechim: locations.user_id -> users.user_id (correct)
--
-- Sabab: JWT tokendan UserId (PHP worker_id) kelib, bu users.user_id hisoblanadi
-- ========================================

BEGIN;

-- Step 1: Drop existing foreign key constraint
ALTER TABLE locations DROP CONSTRAINT IF EXISTS locations_user_id_fkey;

-- Step 2: Add UNIQUE constraint to users.user_id (if not exists)
-- UNIQUE kerak chunki foreign key faqat UNIQUE yoki PRIMARY KEY column'ga ishora qilishi mumkin
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'users_user_id_unique'
    ) THEN
        ALTER TABLE users ADD CONSTRAINT users_user_id_unique UNIQUE (user_id);
    END IF;
END $$;

-- Step 3: Create new foreign key: locations.user_id -> users.user_id
ALTER TABLE locations
    ADD CONSTRAINT locations_user_id_fkey
    FOREIGN KEY (user_id)
    REFERENCES users(user_id)
    ON DELETE CASCADE;

COMMIT;

-- Verify changes
SELECT
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM
    information_schema.table_constraints AS tc
    JOIN information_schema.key_column_usage AS kcu
      ON tc.constraint_name = kcu.constraint_name
      AND tc.table_schema = kcu.table_schema
    JOIN information_schema.constraint_column_usage AS ccu
      ON ccu.constraint_name = tc.constraint_name
      AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_name = 'locations'
  AND kcu.column_name = 'user_id';
