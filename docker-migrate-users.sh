#!/bin/bash
# Docker production database migration script
# Run this on the server where Docker is running

echo "🔧 Migrating users table in Docker database..."

# Find convoy container
CONTAINER_ID=$(docker ps --filter "name=convoy" --format "{{.ID}}" | head -n 1)

if [ -z "$CONTAINER_ID" ]; then
    echo "❌ Convoy container not found!"
    exit 1
fi

echo "✅ Found container: $CONTAINER_ID"

# Run SQL migration
docker exec -i $CONTAINER_ID psql -h 172.17.0.1 -U postgres -d convoydb << 'EOF'
-- Add missing columns
ALTER TABLE users ADD COLUMN IF NOT EXISTS user_id INTEGER;
ALTER TABLE users ADD COLUMN IF NOT EXISTS branch_guid VARCHAR(100);
ALTER TABLE users ADD COLUMN IF NOT EXISTS branch_name VARCHAR(200);
ALTER TABLE users ADD COLUMN IF NOT EXISTS worker_guid VARCHAR(100);
ALTER TABLE users ADD COLUMN IF NOT EXISTS position_id INTEGER;
ALTER TABLE users ADD COLUMN IF NOT EXISTS image VARCHAR(500);
ALTER TABLE users ADD COLUMN IF NOT EXISTS user_type VARCHAR(50);
ALTER TABLE users ADD COLUMN IF NOT EXISTS role VARCHAR(100);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_users_user_id ON users(user_id) WHERE user_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_users_role ON users(role) WHERE role IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_users_phone ON users(phone) WHERE phone IS NOT NULL;

-- Verify
SELECT 'Migration completed!' AS status;
EOF

echo "✅ Migration completed successfully!"
