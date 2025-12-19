#!/bin/bash

# Convoy GPS Tracking System - NuGet Packages Installation Script
# Run this script from the solution root directory

echo "================================================"
echo "Installing NuGet Packages for Convoy Solution"
echo "================================================"

echo ""
echo "[1/4] Installing packages for Convoy.Data..."
cd Convoy.Data
dotnet add package Dapper --version 2.1.35
dotnet add package Npgsql --version 8.0.1
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 8.0.0
cd ..

echo ""
echo "[2/4] Installing packages for Convoy.Service..."
cd Convoy.Service
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 8.0.0
dotnet add package Microsoft.Extensions.Hosting.Abstractions --version 8.0.0
cd ..

echo ""
echo "[3/4] Installing packages for Convoy.Api..."
cd Convoy.Api
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
cd ..

echo ""
echo "[4/4] Adding project references..."

cd Convoy.Data
dotnet add reference ../Convoy.Domain/Convoy.Domain.csproj
cd ..

cd Convoy.Service
dotnet add reference ../Convoy.Data/Convoy.Data.csproj
dotnet add reference ../Convoy.Domain/Convoy.Domain.csproj
cd ..

cd Convoy.Api
dotnet add reference ../Convoy.Service/Convoy.Service.csproj
dotnet add reference ../Convoy.Data/Convoy.Data.csproj
dotnet add reference ../Convoy.Domain/Convoy.Domain.csproj
cd ..

echo ""
echo "================================================"
echo "Building solution..."
echo "================================================"
dotnet build

echo ""
echo "================================================"
echo "Installation completed successfully!"
echo "================================================"
echo ""
echo "Next steps:"
echo "1. Update connection string in Convoy.Api/appsettings.json"
echo "2. Run database-setup.sql in PostgreSQL"
echo "3. Run: cd Convoy.Api && dotnet run"
echo ""
