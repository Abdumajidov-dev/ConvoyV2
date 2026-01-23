-- Create test user for location testing
-- This user will be used to test location creation

-- Check if user with ID 1 exists
DO $$
BEGIN
    -- Insert test user if not exists
    IF NOT EXISTS (SELECT 1 FROM users WHERE id = 1) THEN
        INSERT INTO users (id, name, phone, is_active, created_at, updated_at)
        VALUES (1, 'Test User', '+998901234567', true, NOW(), NOW());

        RAISE NOTICE 'Test user created with ID: 1';
    ELSE
        RAISE NOTICE 'Test user already exists with ID: 1';
    END IF;

    -- Also create user with ID 123 for testing
    IF NOT EXISTS (SELECT 1 FROM users WHERE id = 123) THEN
        INSERT INTO users (id, name, phone, is_active, created_at, updated_at)
        VALUES (123, 'Test User 123', '+998909876543', true, NOW(), NOW());

        RAISE NOTICE 'Test user created with ID: 123';
    ELSE
        RAISE NOTICE 'Test user already exists with ID: 123';
    END IF;
END $$;

-- Show created users
SELECT id, name, phone, is_active FROM users WHERE id IN (1, 123);
