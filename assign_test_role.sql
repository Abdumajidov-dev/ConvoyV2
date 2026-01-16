-- Test uchun user'ga role assign qilish
-- Bu script test user yaratadi va unga Driver role'ni beradi

-- 1. Test user yaratish (agar mavjud bo'lmasa)
-- IMPORTANT: user_id ni o'zgartiring (token ichidagi user_id bilan mos kelishi kerak)
INSERT INTO users (id, name, phone, is_active, created_at)
VALUES (1, 'Test User', '998901234567', true, NOW())
ON CONFLICT (id) DO UPDATE
SET name = 'Test User',
    phone = '998901234567',
    is_active = true,
    updated_at = NOW();

-- 2. Driver role'ni topish
-- Result: 4 (masalan)

-- 3. User'ga Driver role'ni assign qilish
INSERT INTO user_roles (user_id, role_id, assigned_at, created_at)
VALUES (1, 4, NOW(), NOW())
ON CONFLICT (user_id, role_id) DO NOTHING;

-- 4. Verification - User'ning permission'larini ko'rish
SELECT DISTINCT p.resource, p.action, p.name
FROM users u
JOIN user_roles ur ON u.id = ur.user_id
JOIN role_permissions rp ON ur.role_id = rp.role_id
JOIN permissions p ON rp.permission_id = p.id
WHERE u.id = 1 AND p.is_active = true
ORDER BY p.resource, p.action;

-- 5. Verification - User'ning role'larini ko'rish
SELECT r.id, r.name, r.display_name
FROM users u
JOIN user_roles ur ON u.id = ur.user_id
JOIN roles r ON ur.role_id = r.id
WHERE u.id = 1 AND r.is_active = true;
