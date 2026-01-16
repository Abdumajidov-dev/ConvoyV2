@echo off
SET PGPASSWORD=Danger124
"C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -d convoy_db -f create-partitions.sql
pause
