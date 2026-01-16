@echo off
REM Database Setup Script for Convoy GPS Tracking
REM Run this from the solution root directory

SET PGPASSWORD=Danger124
SET PSQL="C:\Program Files\PostgreSQL\16\bin\psql.exe"

echo ================================================
echo Convoy Database Setup
echo ================================================
echo.

echo Running SQL script...
%PSQL% -U postgres -d convoy_db -f database-setup.sql

echo.
echo ================================================
echo Checking created partitions...
echo ================================================
%PSQL% -U postgres -d convoy_db -c "SELECT tablename FROM pg_tables WHERE tablename LIKE 'locations_%%' ORDER BY tablename;"

echo.
echo ================================================
echo Checking users table...
echo ================================================
%PSQL% -U postgres -d convoy_db -c "SELECT COUNT(*) as user_count FROM users;"

echo.
echo ================================================
echo Setup completed!
echo ================================================
echo.
echo Next steps:
echo 1. cd Convoy.Api
echo 2. dotnet run
echo 3. Open: https://localhost:5001/swagger
echo.

pause
