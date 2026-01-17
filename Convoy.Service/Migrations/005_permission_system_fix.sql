-- ================================================
-- Permission System Migration Script
-- Convoy GPS Tracking - ASP.NET Core Identity + Permission
-- ================================================

-- Force re-run if this migration was partially applied before
-- This ensures idempotency even if previous run failed midway
DO $$
BEGIN
    -- If permissions table doesn't exist but migration was marked as applied,
    -- remove it from migrations table to allow re-run
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'permissions') THEN
        DELETE FROM __migrations WHERE migration_name = '002_permission_system';
        RAISE NOTICE 'Removed partial migration 002_permission_system from tracking';
    END IF;
END $$;

-- 1. Create roles table
CREATE TABLE IF NOT EXISTS roles (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    display_name VARCHAR(200) NOT NULL,
    description VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_roles_name ON roles(name);
CREATE INDEX IF NOT EXISTS idx_roles_is_active ON roles(is_active);

-- 2. Create permissions table
CREATE TABLE IF NOT EXISTS permissions (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    display_name VARCHAR(200) NOT NULL,
    resource VARCHAR(50) NOT NULL,
    action VARCHAR(50) NOT NULL,
    description VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_permissions_name ON permissions(name);
CREATE INDEX IF NOT EXISTS idx_permissions_resource_action ON permissions(resource, action);
CREATE INDEX IF NOT EXISTS idx_permissions_is_active ON permissions(is_active);

-- 3. Create user_roles junction table
CREATE TABLE IF NOT EXISTS user_roles (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    assigned_by BIGINT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ,
    CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    CONSTRAINT uk_user_roles UNIQUE (user_id, role_id)
);

CREATE INDEX IF NOT EXISTS idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_role_id ON user_roles(role_id);

-- 4. Create role_permissions junction table
CREATE TABLE IF NOT EXISTS role_permissions (
    id BIGSERIAL PRIMARY KEY,
    role_id BIGINT NOT NULL,
    permission_id BIGINT NOT NULL,
    granted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    granted_by BIGINT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ,
    CONSTRAINT fk_role_permissions_role FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE,
    CONSTRAINT fk_role_permissions_permission FOREIGN KEY (permission_id) REFERENCES permissions(id) ON DELETE CASCADE,
    CONSTRAINT uk_role_permissions UNIQUE (role_id, permission_id)
);

CREATE INDEX IF NOT EXISTS idx_role_permissions_role_id ON role_permissions(role_id);
CREATE INDEX IF NOT EXISTS idx_role_permissions_permission_id ON role_permissions(permission_id);

-- ================================================
-- Seed Data - Permissions
-- ================================================

-- Users permissions
INSERT INTO permissions (name, display_name, resource, action, description, is_active, created_at)
VALUES
    ('users.view', 'View Users', 'users', 'view', 'Foydalanuvchilar ro''yxatini ko''rish', true, NOW()),
    ('users.create', 'Create Users', 'users', 'create', 'Yangi foydalanuvchi yaratish', true, NOW()),
    ('users.update', 'Update Users', 'users', 'update', 'Foydalanuvchi ma''lumotlarini o''zgartirish', true, NOW()),
    ('users.delete', 'Delete Users', 'users', 'delete', 'Foydalanuvchini o''chirish', true, NOW()),
    ('users.manage', 'Manage Users', 'users', 'manage', 'Foydalanuvchilarni to''liq boshqarish', true, NOW())
ON CONFLICT (name) DO NOTHING;

-- Locations permissions
INSERT INTO permissions (name, display_name, resource, action, description, is_active, created_at)
VALUES
    ('locations.view', 'View Locations', 'locations', 'view', 'O''z lokatsiyalarini ko''rish', true, NOW()),
    ('locations.create', 'Create Locations', 'locations', 'create', 'Yangi lokatsiya yaratish', true, NOW()),
    ('locations.update', 'Update Locations', 'locations', 'update', 'Lokatsiya ma''lumotlarini o''zgartirish', true, NOW()),
    ('locations.delete', 'Delete Locations', 'locations', 'delete', 'Lokatsiyani o''chirish', true, NOW()),
    ('locations.view_all', 'View All Locations', 'locations', 'view_all', 'Barcha foydalanuvchilarning lokatsiyalarini ko''rish', true, NOW()),
    ('locations.export', 'Export Locations', 'locations', 'export', 'Lokatsiyalarni export qilish', true, NOW())
ON CONFLICT (name) DO NOTHING;

-- Reports permissions
INSERT INTO permissions (name, display_name, resource, action, description, is_active, created_at)
VALUES
    ('reports.view', 'View Reports', 'reports', 'view', 'Hisobotlarni ko''rish', true, NOW()),
    ('reports.export', 'Export Reports', 'reports', 'export', 'Hisobotlarni export qilish', true, NOW()),
    ('reports.create', 'Create Reports', 'reports', 'create', 'Yangi hisobot yaratish', true, NOW())
ON CONFLICT (name) DO NOTHING;

-- Roles permissions
INSERT INTO permissions (name, display_name, resource, action, description, is_active, created_at)
VALUES
    ('roles.view', 'View Roles', 'roles', 'view', 'Rollarni ko''rish', true, NOW()),
    ('roles.create', 'Create Roles', 'roles', 'create', 'Yangi rol yaratish', true, NOW()),
    ('roles.update', 'Update Roles', 'roles', 'update', 'Rol ma''lumotlarini o''zgartirish', true, NOW()),
    ('roles.delete', 'Delete Roles', 'roles', 'delete', 'Rolni o''chirish', true, NOW()),
    ('roles.assign_permissions', 'Assign Permissions to Roles', 'roles', 'assign_permissions', 'Rollarga ruxsat berish', true, NOW())
ON CONFLICT (name) DO NOTHING;

-- Permissions management
INSERT INTO permissions (name, display_name, resource, action, description, is_active, created_at)
VALUES
    ('permissions.view', 'View Permissions', 'permissions', 'view', 'Ruxsatlarni ko''rish', true, NOW()),
    ('permissions.assign', 'Assign Permissions', 'permissions', 'assign', 'Ruxsatlarni tayinlash', true, NOW())
ON CONFLICT (name) DO NOTHING;

-- Dashboard permissions
INSERT INTO permissions (name, display_name, resource, action, description, is_active, created_at)
VALUES
    ('dashboard.view_own', 'View Own Dashboard', 'dashboard', 'view_own', 'O''z dashboard''ini ko''rish', true, NOW()),
    ('dashboard.view_all', 'View All Dashboard', 'dashboard', 'view_all', 'Barcha dashboard''larni ko''rish', true, NOW()),
    ('dashboard.view_statistics', 'View Statistics', 'dashboard', 'view_statistics', 'Statistikalarni ko''rish', true, NOW())
ON CONFLICT (name) DO NOTHING;

-- Settings permissions
INSERT INTO permissions (name, display_name, resource, action, description, is_active, created_at)
VALUES
    ('settings.view', 'View Settings', 'settings', 'view', 'Sozlamalarni ko''rish', true, NOW()),
    ('settings.update', 'Update Settings', 'settings', 'update', 'Sozlamalarni o''zgartirish', true, NOW())
ON CONFLICT (name) DO NOTHING;

-- ================================================
-- Seed Data - Roles
-- ================================================

INSERT INTO roles (name, display_name, description, is_active, created_at)
VALUES
    ('SuperAdmin', 'Super Administrator', 'Sistema super administratori - barcha ruxsatlarga ega', true, NOW()),
    ('Admin', 'Administrator', 'Sistema administratori - ko''pgina ruxsatlarga ega', true, NOW()),
    ('Manager', 'Manager', 'Menejer - foydalanuvchilar va lokatsiyalarni boshqaradi', true, NOW()),
    ('Driver', 'Driver', 'Haydovchi - faqat o''z ma''lumotlarini ko''radi va yaratadi', true, NOW()),
    ('Viewer', 'Viewer', 'Ko''ruvchi - faqat ma''lumotlarni ko''rish huquqi', true, NOW())
ON CONFLICT (name) DO NOTHING;

-- ================================================
-- Seed Data - Role-Permission Relationships
-- ================================================

-- SuperAdmin - ALL PERMISSIONS
INSERT INTO role_permissions (role_id, permission_id, granted_at, created_at)
SELECT r.id, p.id, NOW(), NOW()
FROM roles r
CROSS JOIN permissions p
WHERE r.name = 'SuperAdmin' AND p.is_active = true
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Admin - Most permissions
INSERT INTO role_permissions (role_id, permission_id, granted_at, created_at)
SELECT r.id, p.id, NOW(), NOW()
FROM roles r
CROSS JOIN permissions p
WHERE r.name = 'Admin'
  AND p.name IN (
    'users.view', 'users.create', 'users.update',
    'locations.view', 'locations.view_all', 'locations.export',
    'reports.view', 'reports.export', 'reports.create',
    'dashboard.view_all', 'dashboard.view_statistics',
    'settings.view'
)
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Manager
INSERT INTO role_permissions (role_id, permission_id, granted_at, created_at)
SELECT r.id, p.id, NOW(), NOW()
FROM roles r
CROSS JOIN permissions p
WHERE r.name = 'Manager'
  AND p.name IN (
    'users.view',
    'locations.view', 'locations.view_all', 'locations.export',
    'reports.view', 'reports.export',
    'dashboard.view_all', 'dashboard.view_statistics'
)
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Driver
INSERT INTO role_permissions (role_id, permission_id, granted_at, created_at)
SELECT r.id, p.id, NOW(), NOW()
FROM roles r
CROSS JOIN permissions p
WHERE r.name = 'Driver'
  AND p.name IN (
    'locations.view', 'locations.create',
    'dashboard.view_own'
)
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Viewer
INSERT INTO role_permissions (role_id, permission_id, granted_at, created_at)
SELECT r.id, p.id, NOW(), NOW()
FROM roles r
CROSS JOIN permissions p
WHERE r.name = 'Viewer'
  AND p.name IN (
    'users.view',
    'locations.view',
    'reports.view',
    'dashboard.view_own'
)
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- ================================================
-- Verification Queries (commented out - not needed in migration)
-- ================================================

-- -- Check created tables
-- SELECT table_name
-- FROM information_schema.tables
-- WHERE table_schema = 'public'
--   AND table_name IN ('roles', 'permissions', 'user_roles', 'role_permissions')
-- ORDER BY table_name;

-- -- Count permissions
-- SELECT COUNT(*) as total_permissions FROM permissions;

-- -- Count roles
-- SELECT COUNT(*) as total_roles FROM roles;

-- -- Count role-permission relationships
-- SELECT r.name as role_name, COUNT(rp.id) as permission_count
-- FROM roles r
-- LEFT JOIN role_permissions rp ON r.id = rp.role_id
-- GROUP BY r.id, r.name
-- ORDER BY r.name;

-- -- Show all permissions by resource
-- SELECT resource, COUNT(*) as permission_count
-- FROM permissions
-- GROUP BY resource
-- ORDER BY resource;
