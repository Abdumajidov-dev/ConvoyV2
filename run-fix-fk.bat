@echo off
set PGPASSWORD=GarantDockerPass
psql -h 10.21.61.51 -p 5432 -U postgres -d convoydb -c "ALTER TABLE locations DROP CONSTRAINT IF EXISTS locations_user_id_fkey;"
psql -h 10.21.61.51 -p 5432 -U postgres -d convoydb -c "ALTER TABLE locations ADD CONSTRAINT locations_user_id_fkey FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE;"
echo.
echo Foreign key constraint fixed!
pause
