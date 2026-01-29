#!/bin/bash
# Apply migration to Docker production database

echo "🔧 Applying AddUserPhpApiFields migration..."

# Find convoy container
CONTAINER_ID=$(docker ps --filter "ancestor=jm7uz/convoy:latest" --format "{{.ID}}" | head -n 1)

if [ -z "$CONTAINER_ID" ]; then
    echo "❌ Convoy container not found!"
    exit 1
fi

echo "✅ Found container: $CONTAINER_ID"

# Apply migration
echo "📝 Running SQL migration..."
docker exec -i $CONTAINER_ID sh -c "PGPASSWORD=GarantDockerPass psql -h 172.17.0.1 -U postgres -d convoydb" << 'EOF'
-- Add columns
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

-- Insert migration history
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260128_AddUserPhpApiFields', '8.0.0')
ON CONFLICT DO NOTHING;

-- Verify
\dt users
EOF

echo "✅ Migration completed!"
echo "🔄 Restarting container..."
docker restart $CONTAINER_ID

echo "✅ Done! Container restarted."
