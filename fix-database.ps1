# Fix Database Issues
# 1. Fix FK constraint
# 2. Create missing tables

$env:PGPASSWORD = "GarantDockerPass"
$host = "10.21.61.51"
$port = "5432"
$user = "postgres"
$db = "convoydb"

Write-Host "=== Fixing Foreign Key Constraint ===" -ForegroundColor Yellow

# Drop existing FK
$sql1 = "ALTER TABLE locations DROP CONSTRAINT IF EXISTS locations_user_id_fkey;"
Write-Host "Dropping old FK constraint..."
psql -h $host -p $port -U $user -d $db -c $sql1

# Create new FK
$sql2 = "ALTER TABLE locations ADD CONSTRAINT locations_user_id_fkey FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE;"
Write-Host "Creating new FK constraint (referencing users.user_id)..."
psql -h $host -p $port -U $user -d $db -c $sql2

Write-Host ""
Write-Host "=== Creating Missing Tables ===" -ForegroundColor Yellow

# Run the notification system SQL script
Write-Host "Running add-notification-system.sql..."
psql -h $host -p $port -U $user -d $db -f "add-notification-system.sql"

Write-Host ""
Write-Host "=== Verification ===" -ForegroundColor Green

# Verify FK
$verifyFK = "SELECT conname, confrelid::regclass AS referenced_table, a.attname AS column_name, af.attname AS foreign_column FROM pg_constraint c JOIN pg_attribute a ON a.attnum = ANY(c.conkey) AND a.attrelid = c.conrelid JOIN pg_attribute af ON af.attnum = ANY(c.confkey) AND af.attrelid = c.confrelid WHERE c.conrelid = 'locations'::regclass AND c.contype = 'f';"
Write-Host "Verifying FK constraint..."
psql -h $host -p $port -U $user -d $db -c $verifyFK

# Verify tables
$verifyTables = "SELECT tablename FROM pg_tables WHERE tablename IN ('user_status_reports', 'admin_notifications', 'device_tokens') ORDER BY tablename;"
Write-Host "Verifying tables exist..."
psql -h $host -p $port -U $user -d $db -c $verifyTables

Write-Host ""
Write-Host "=== DONE ===" -ForegroundColor Green
