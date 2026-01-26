-- Migration Script: PHP API Full Integration
-- Bu script users table'ga PHP API integration uchun kerakli columnlarni qo'shadi

-- ============================================
-- USERS TABLE MIGRATION
-- ============================================

-- user_id column qo'shish (agar mavjud bo'lmasa)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'user_id') THEN
        ALTER TABLE users ADD COLUMN user_id INTEGER UNIQUE;
        CREATE UNIQUE INDEX idx_users_user_id ON users(user_id) WHERE user_id IS NOT NULL;
        RAISE NOTICE 'Added user_id column to users table';
    ELSE
        RAISE NOTICE 'user_id column already exists';
    END IF;
END $$;

-- branch_guid column qo'shish (agar mavjud bo'lmasa)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'branch_guid') THEN
        ALTER TABLE users ADD COLUMN branch_guid VARCHAR(100);
        RAISE NOTICE 'Added branch_guid column to users table';
    ELSE
        RAISE NOTICE 'branch_guid column already exists';
    END IF;
END $$;

-- worker_guid column qo'shish (agar mavjud bo'lmasa)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'worker_guid') THEN
        ALTER TABLE users ADD COLUMN worker_guid VARCHAR(100);
        RAISE NOTICE 'Added worker_guid column to users table';
    ELSE
        RAISE NOTICE 'worker_guid column already exists';
    END IF;
END $$;

-- position_id column qo'shish (agar mavjud bo'lmasa)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'position_id') THEN
        ALTER TABLE users ADD COLUMN position_id INTEGER;
        RAISE NOTICE 'Added position_id column to users table';
    ELSE
        RAISE NOTICE 'position_id column already exists';
    END IF;
END $$;

-- image column qo'shish (agar mavjud bo'lmasa)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'image') THEN
        ALTER TABLE users ADD COLUMN image VARCHAR(500);
        RAISE NOTICE 'Added image column to users table';
    ELSE
        RAISE NOTICE 'image column already exists';
    END IF;
END $$;

-- ============================================
-- SUMMARY
-- ============================================

SELECT
    'Migration completed successfully!' as status,
    COUNT(*) as total_users,
    COUNT(user_id) as users_with_php_id
FROM users;
