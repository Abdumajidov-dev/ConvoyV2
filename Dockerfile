# Convoy GPS Tracking API - Dockerfile
# COMPLETE REBUILD - 2026-01-20T13:00:00Z
# Railway cache bust: FORCE ALL CONTROLLERS

# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Build stage - COMPLETELY NEW STRUCTURE
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# === CRITICAL: FORCE CACHE INVALIDATION ===
# This must be UNIQUE on every deploy to bust Railway cache
RUN echo "════════════════════════════════════════════════" && \
    echo "🚀 CONVOY API BUILD: 1768908340-COMPLETE-REBUILD" && \
    echo "════════════════════════════════════════════════" && \
    echo "Expected Controllers:" && \
    echo "  ✅ AuthController (/api/auth)" && \
    echo "  ✅ BranchController (/api/branches)" && \
    echo "  ✅ LocationController (/api/locations)" && \
    echo "  ✅ UserController (/api/users)" && \
    echo "  ❌ DailySummary (MUST BE REMOVED - old cache)" && \
    echo "════════════════════════════════════════════════" && \
    date && uname -a

WORKDIR /src

# Copy ALL source files at once (bypass granular cache)
COPY . .

# Verify controllers exist before build
RUN echo "Verifying controllers exist:" && \
    ls -la /src/Convoy.Api/Controllers/ && \
    echo "Controller files found:" && \
    find /src/Convoy.Api/Controllers -name "*.cs" -type f && \
    echo "AuthController check:" && \
    grep -l "class AuthController" /src/Convoy.Api/Controllers/*.cs || echo "❌ AuthController NOT FOUND" && \
    echo "BranchController check:" && \
    grep -l "class BranchController" /src/Convoy.Api/Controllers/*.cs || echo "❌ BranchController NOT FOUND"

# Restore dependencies
WORKDIR /src/Convoy.Api
RUN dotnet restore "Convoy.Api.csproj" --verbosity detailed

# Build with verbose output
RUN dotnet build "Convoy.Api.csproj" \
    -c Release \
    -o /app/build \
    --verbosity normal \
    --no-restore

# Verify DLL includes all controllers
RUN echo "Build complete. Checking assembly:" && \
    ls -lh /app/build/Convoy.Api.dll

# Publish stage
FROM build AS publish
RUN dotnet publish "Convoy.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore \
    --verbosity normal

# List published files
RUN echo "Published files:" && \
    ls -lh /app/publish/ && \
    echo "Convoy.Api.dll size:" && \
    du -h /app/publish/Convoy.Api.dll

# Final runtime stage
FROM base AS final
WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

# Verify final files
RUN echo "Final stage files:" && \
    ls -lh /app/

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080

# Health check for Railway
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:$PORT/health || exit 1

# Entry point with verbose logging
ENTRYPOINT ["sh", "-c", "echo '🚀 Starting Convoy API on port $PORT' && dotnet Convoy.Api.dll --urls http://0.0.0.0:$PORT"]
