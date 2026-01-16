# Convoy Database Setup PowerShell Script

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Convoy GPS Tracking - Database Setup" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

$env:PGPASSWORD = "Danger124"
$psqlPath = "C:\Program Files\PostgreSQL\16\bin\psql.exe"
$sqlFile = "database-setup.sql"

Write-Host "Running SQL script..." -ForegroundColor Yellow
& $psqlPath -U postgres -d convoy_db -f $sqlFile

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Checking created partitions..." -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
& $psqlPath -U postgres -d convoy_db -c "SELECT tablename FROM pg_tables WHERE tablename LIKE 'locations_%' ORDER BY tablename;"

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Checking users table..." -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
& $psqlPath -U postgres -d convoy_db -c "SELECT COUNT(*) as user_count FROM users;"

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "Setup completed!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. cd Convoy.Api" -ForegroundColor White
Write-Host "2. dotnet run" -ForegroundColor White
Write-Host "3. Open: https://localhost:5001/swagger" -ForegroundColor White
Write-Host ""
