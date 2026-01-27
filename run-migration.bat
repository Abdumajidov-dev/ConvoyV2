@echo off
echo ========================================
echo Running Migration: Add PHP Token Fields
echo ========================================
echo.

REM Check if psql is installed
where psql >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] psql not found!
    echo.
    echo Please install PostgreSQL or use one of these options:
    echo   1. Use Railway Dashboard SQL Console
    echo   2. Use pgAdmin
    echo   3. Use EF Core Migration (recommended)
    echo.
    echo Option 3: EF Core Migration
    echo Run this command:
    echo   dotnet ef database update --project Convoy.Data --startup-project Convoy.Api
    echo.
    pause
    exit /b 1
)

REM Connection details
set PGHOST=crossover.proxy.rlwy.net
set PGPORT=31579
set PGDATABASE=railway
set PGUSER=postgres
set PGPASSWORD=YrSIsEidlvQRLXLjpMkdHmDnsWsiqHkH

echo Running migration SQL...
echo.

psql -h %PGHOST% -p %PGPORT% -U %PGUSER% -d %PGDATABASE% -f add-token-fields-migration.sql

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Migration completed successfully!
    echo ========================================
) else (
    echo.
    echo ========================================
    echo Migration FAILED!
    echo ========================================
)

pause
