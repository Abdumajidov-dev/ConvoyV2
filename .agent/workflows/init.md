---
description: Initialize ConvoyV2 project - complete setup automation
---

# ConvoyV2 Project Initialization

This workflow automates the complete setup of the ConvoyV2 GPS tracking system.

## Prerequisites Check

1. Verify .NET 8 SDK is installed
```powershell
dotnet --version
```

2. Verify PostgreSQL is installed and running
```powershell
pg_isready -U postgres
```

## Step 1: Install NuGet Packages

// turbo
3. Install all required NuGet packages using the automated script
```powershell
.\install-packages.bat
```

## Step 2: Database Setup

4. Check if PostgreSQL service is running
```powershell
Get-Service -Name postgresql*
```

5. Create the database (if it doesn't exist)
```powershell
psql -U postgres -c "CREATE DATABASE convoy_db;"
```

// turbo
6. Run the database setup script
```powershell
.\run-database-setup.bat
```

## Step 3: Environment Configuration

7. Check if appsettings.json exists and has correct connection string
```powershell
Get-Content .\Convoy.Api\appsettings.json | Select-String "DefaultConnection"
```

8. If connection string needs updating, edit the file:
```
Edit Convoy.Api/appsettings.json and update the DefaultConnection with your PostgreSQL password
```

## Step 4: Generate Encryption Keys (Optional)

// turbo
9. Generate encryption keys if needed for the encryption features
```powershell
.\generate-encryption-keys.ps1
```

## Step 5: Build Solution

// turbo
10. Build the entire solution to verify everything compiles
```powershell
dotnet build canvoy.sln
```

## Step 6: Run Database Migrations

// turbo
11. Apply any pending migrations
```powershell
cd Convoy.Api
dotnet ef database update
cd ..
```

## Step 7: Verify Database Setup

12. Check that tables and partitions were created
```powershell
psql -U postgres -d convoy_db -c "SELECT tablename FROM pg_tables WHERE schemaname = 'public' ORDER BY tablename;"
```

13. Verify partition creation function exists
```powershell
psql -U postgres -d convoy_db -c "SELECT proname FROM pg_proc WHERE proname = 'create_location_partition';"
```

## Step 8: Start the Application

// turbo
14. Run the API application
```powershell
cd Convoy.Api
dotnet run
```

## Step 9: Verify Application

15. Once the application starts, open Swagger UI in your browser:
```
https://localhost:5001/swagger
```

16. Test the health endpoint:
```powershell
curl https://localhost:5001/api/location/user/1/last?count=10
```

## Verification Checklist

After running this workflow, verify:
- ✅ All NuGet packages installed
- ✅ Database `convoy_db` created
- ✅ Tables created: `users`, `locations`
- ✅ Partition function `create_location_partition` exists
- ✅ Monthly partitions created (locations_XX_YYYY)
- ✅ Solution builds without errors
- ✅ API starts successfully
- ✅ Swagger UI accessible at https://localhost:5001/swagger
- ✅ Logs show "PartitionMaintenanceService starting..."

## Troubleshooting

**PostgreSQL not running:**
```powershell
Start-Service postgresql-x64-16
```

**Connection errors:**
- Check password in appsettings.json
- Verify PostgreSQL is listening on port 5432
- Check firewall settings

**Build errors:**
```powershell
dotnet clean
dotnet restore
dotnet build
```

**Missing partitions:**
```powershell
psql -U postgres -d convoy_db -c "SELECT create_location_partition('2025-12-01'::DATE);"
```

## Next Steps

After initialization:
1. Review `QUICK-START.md` for API usage examples
2. Check `API-EXAMPLES.http` for sample requests
3. Read `FLUTTER_BACKGROUND_GEOLOCATION_INTEGRATION.md` for mobile integration
4. Review `PERMISSION_SYSTEM_GUIDE.md` for permission management
5. See `ENCRYPTION_QUICKSTART.md` for encryption features
