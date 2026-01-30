-- ============================================
-- FIX: Location Foreign Key Constraint
-- ============================================
-- Problem: locations.user_id references users.id (database PK)
-- Solution: Change to reference users.user_id (PHP worker_id)
-- ============================================

-- Step 1: Drop existing foreign key constraint
ALTER TABLE locations DROP CONSTRAINT IF EXISTS locations_user_id_fkey;

-- Step 2: Create new foreign key to users.user_id instead of users.id
ALTER TABLE locations
ADD CONSTRAINT locations_user_id_fkey
FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE;

-- Success message
SELECT 'Foreign key constraint fixed successfully!' AS status;
