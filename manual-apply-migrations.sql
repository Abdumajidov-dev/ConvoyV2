-- 1. Mark old migration as applied (to skip it)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260130115009_AddUserStatusReportAndAdminNotification', '8.0.0')
ON CONFLICT DO NOTHING;

-- 2. Create user_stopped_reports table manually
CREATE TABLE IF NOT EXISTS user_stopped_reports (
    id BIGSERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL,
    location_id BIGINT NOT NULL,
    latitude DECIMAL(10, 8) NOT NULL,
    longitude DECIMAL(11, 8) NOT NULL,
    stopped_at TIMESTAMPTZ NOT NULL,
    stopped_duration_minutes INTEGER NOT NULL,
    reason VARCHAR(500) NOT NULL DEFAULT '',
    is_resolved BOOLEAN NOT NULL DEFAULT FALSE,
    resolved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ
);

-- 3. Create indexes
CREATE INDEX IF NOT EXISTS "IX_user_stopped_reports_user_id" ON user_stopped_reports(user_id);
CREATE INDEX IF NOT EXISTS "IX_user_stopped_reports_location_id" ON user_stopped_reports(location_id);
CREATE INDEX IF NOT EXISTS "IX_user_stopped_reports_created_at" ON user_stopped_reports(created_at);

-- 4. Mark new migration as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260211060257_AddUserStoppedReportsTable', '8.0.0')
ON CONFLICT DO NOTHING;

-- Verification query
SELECT tablename, schemaname
FROM pg_tables
WHERE tablename = 'user_stopped_reports';
