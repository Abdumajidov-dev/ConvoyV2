-- Token Blacklist table (logout qilingan tokenlar)
-- JWT token invalidation uchun ishlatiladi

CREATE TABLE IF NOT EXISTS token_blacklist (
    id SERIAL PRIMARY KEY,
    token_jti VARCHAR(100) NOT NULL UNIQUE,  -- JWT ID (jti claim)
    user_id INTEGER NOT NULL,
    blacklisted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,  -- Token'ning asl expiry vaqti
    reason VARCHAR(50),  -- 'logout', 'security', 'admin_revoke', etc.

    -- Indexlar
    CONSTRAINT fk_token_blacklist_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Index for fast lookups
CREATE INDEX IF NOT EXISTS idx_token_blacklist_jti ON token_blacklist(token_jti);
CREATE INDEX IF NOT EXISTS idx_token_blacklist_user ON token_blacklist(user_id);
CREATE INDEX IF NOT EXISTS idx_token_blacklist_expires ON token_blacklist(expires_at);

-- Cleanup eski tokenlarni o'chirish uchun function
CREATE OR REPLACE FUNCTION cleanup_expired_blacklisted_tokens()
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM token_blacklist
    WHERE expires_at < NOW();

    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- Comment
COMMENT ON TABLE token_blacklist IS 'Logout qilingan yoki bekor qilingan JWT tokenlar';
COMMENT ON COLUMN token_blacklist.token_jti IS 'JWT ID (jti claim) - unique identifier';
COMMENT ON COLUMN token_blacklist.reason IS 'Blacklist qilish sababi: logout, security, admin_revoke';
