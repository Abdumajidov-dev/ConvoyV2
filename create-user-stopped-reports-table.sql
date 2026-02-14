-- Create user_stopped_reports table
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
    deleted_at TIMESTAMPTZ
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_user_stopped_reports_user_id ON user_stopped_reports(user_id);
CREATE INDEX IF NOT EXISTS idx_user_stopped_reports_location_id ON user_stopped_reports(location_id);
CREATE INDEX IF NOT EXISTS idx_user_stopped_reports_created_at ON user_stopped_reports(created_at);
CREATE INDEX IF NOT EXISTS idx_user_stopped_reports_is_resolved ON user_stopped_reports(is_resolved);

COMMENT ON TABLE user_stopped_reports IS 'User 1 soatdan ko''p to''xtab qolganda sababini saqlash uchun';
COMMENT ON COLUMN user_stopped_reports.user_id IS 'PHP API worker_id (int)';
COMMENT ON COLUMN user_stopped_reports.location_id IS 'Qaysi locationda to''xtagan';
COMMENT ON COLUMN user_stopped_reports.stopped_at IS 'Qachon to''xtagan';
COMMENT ON COLUMN user_stopped_reports.stopped_duration_minutes IS 'Necha daqiqa to''xtagan';
COMMENT ON COLUMN user_stopped_reports.reason IS 'Sababi (internet uzildi, aktiv emas, manual stop)';
COMMENT ON COLUMN user_stopped_reports.is_resolved IS 'User harakatni davom ettirganda true bo''ladi';
COMMENT ON COLUMN user_stopped_reports.resolved_at IS 'Qachon hal qilingan';
