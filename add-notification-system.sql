-- ====================================================
-- NOTIFICATION SYSTEM - User Location Monitoring
-- ====================================================
-- Bu script user'larni location post qilish holatini kuzatish va
-- admin'larga notification yuborish uchun jadvallar yaratadi
-- ====================================================

-- 1. device_tokens jadvali - User'larning Firebase device token'larini saqlash
CREATE TABLE IF NOT EXISTS device_tokens (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    token VARCHAR(500) NOT NULL,
    device_system VARCHAR(20) NOT NULL,
    model VARCHAR(100) NOT NULL,
    device_id VARCHAR(100) NOT NULL,
    is_physical_device BOOLEAN NOT NULL DEFAULT true,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ,

    -- Foreign Key (references users.user_id - PHP worker_id)
    CONSTRAINT device_tokens_user_id_fkey FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_device_tokens_user_id ON device_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_device_tokens_device_id ON device_tokens(device_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_device_tokens_user_device ON device_tokens(user_id, device_id);

-- 2. user_status_reports jadvali - User'ning location post qilish holatini kuzatish
CREATE TABLE IF NOT EXISTS user_status_reports (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    last_location_time TIMESTAMPTZ,
    last_notified_at TIMESTAMPTZ,
    offline_duration_minutes INTEGER NOT NULL DEFAULT 0,
    is_notified BOOLEAN NOT NULL DEFAULT false,
    notification_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ,

    -- Foreign Key (references users.user_id - PHP worker_id)
    CONSTRAINT user_status_reports_user_id_fkey FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_user_status_reports_user_id ON user_status_reports(user_id);
CREATE INDEX IF NOT EXISTS idx_user_status_reports_last_location_time ON user_status_reports(last_location_time);
CREATE INDEX IF NOT EXISTS idx_user_status_reports_last_notified_at ON user_status_reports(last_notified_at);

-- 3. admin_notifications jadvali - Admin'larga yuborilgan notification'larni saqlash
CREATE TABLE IF NOT EXISTS admin_notifications (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    admin_user_id BIGINT NOT NULL,
    notification_type VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message VARCHAR(1000) NOT NULL,
    offline_duration_minutes INTEGER NOT NULL DEFAULT 0,
    is_sent BOOLEAN NOT NULL DEFAULT false,
    sent_at TIMESTAMPTZ,
    is_read BOOLEAN NOT NULL DEFAULT false,
    read_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ,

    -- Foreign Keys (references users.user_id - PHP worker_id)
    CONSTRAINT admin_notifications_user_id_fkey FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    CONSTRAINT admin_notifications_admin_user_id_fkey FOREIGN KEY (admin_user_id) REFERENCES users(user_id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS idx_admin_notifications_user_id ON admin_notifications(user_id);
CREATE INDEX IF NOT EXISTS idx_admin_notifications_admin_user_id ON admin_notifications(admin_user_id);
CREATE INDEX IF NOT EXISTS idx_admin_notifications_is_sent ON admin_notifications(is_sent);
CREATE INDEX IF NOT EXISTS idx_admin_notifications_is_read ON admin_notifications(is_read);
CREATE INDEX IF NOT EXISTS idx_admin_notifications_created_at ON admin_notifications(created_at);

-- ====================================================
-- Verification queries (ishlatib ko'rish uchun)
-- ====================================================

-- Jadvallar yaratilganini tekshirish
SELECT tablename FROM pg_tables WHERE tablename IN ('device_tokens', 'user_status_reports', 'admin_notifications');

-- Device tokens sonini ko'rish
-- SELECT user_id, COUNT(*) as token_count FROM device_tokens WHERE is_active = true GROUP BY user_id;

-- User status reports ko'rish
-- SELECT u.name, usr.last_location_time, usr.offline_duration_minutes, usr.is_notified, usr.notification_count
-- FROM user_status_reports usr
-- JOIN users u ON usr.user_id = u.id
-- ORDER BY usr.offline_duration_minutes DESC;

-- Admin notification'larni ko'rish
-- SELECT an.*, u.name as user_name, a.name as admin_name
-- FROM admin_notifications an
-- JOIN users u ON an.user_id = u.id
-- JOIN users a ON an.admin_user_id = a.id
-- ORDER BY an.created_at DESC
-- LIMIT 50;

-- ====================================================
-- IMPORTANT NOTES:
-- ====================================================
-- 1. CheckLocationCreatedBackrounService har 1 minutda ishga tushadi
-- 2. User 20, 40, 60, 80, 100, 120 daqiqadan beri offline bo'lsa notification yuboriladi
-- 3. Notification faqat role='admin_unduruv' bo'lgan user'larga yuboriladi
-- 4. Firebase Admin SDK ishlatiladi (firebase-adminsdk.json kerak)
-- 5. Device token'lar login qilganda avtomatik saqlanadi
