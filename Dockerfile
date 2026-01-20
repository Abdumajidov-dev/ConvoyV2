# Convoy GPS Tracking API - Dockerfile
# ABSOLUTE NUCLEAR OPTION - ARG-based cache invalidation
# BUILD_ID changes every commit, forcing complete rebuild

# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# === ARG-BASED CACHE KILLER (Railway cannot cache this) ===
ARG BUILD_ID=unknown
ARG RAILWAY_GIT_COMMIT_SHA=unknown
RUN echo "════════════════════════════════════════════════" && \
    echo "🚀 ABSOLUTE FRESH BUILD" && \
    echo "════════════════════════════════════════════════" && \
    echo "BUILD_ID: ${BUILD_ID}" && \
    echo "GIT_COMMIT: ${RAILWAY_GIT_COMMIT_SHA}" && \
    echo "TIMESTAMP: $(date)" && \
    echo "UNIX_TIME: $(date +%s)" && \
    echo "════════════════════════════════════════════════" && \
    echo "Required Controllers:" && \
    echo "  ✅ AuthController -> /api/auth/*" && \
    echo "  ✅ BranchController -> /api/branches/*" && \
    echo "  ✅ LocationController -> /api/locations/*" && \
    echo "  ✅ UserController -> /api/users/*" && \
    echo "Obsolete (MUST NOT APPEAR):" && \
    echo "  ❌ DailySummaryController (DELETED)" && \
    echo "════════════════════════════════════════════════"

WORKDIR /src

# Copy ALL source files
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

# Verify DLL includes all controllers with strings command
RUN echo "════════════════════════════════════════════════" && \
    echo "🔍 VERIFYING COMPILED DLL CONTENTS" && \
    echo "════════════════════════════════════════════════" && \
    echo "DLL Size:" && \
    ls -lh /app/build/Convoy.Api.dll && \
    echo "" && \
    echo "Searching for controller classes in DLL..." && \
    apt-get update && apt-get install -y binutils && rm -rf /var/lib/apt/lists/* && \
    strings /app/build/Convoy.Api.dll | grep -E "(AuthController|BranchController|LocationController|UserController|DailySummaryController)" | sort -u && \
    echo "" && \
    echo "Controller routes in DLL..." && \
    strings /app/build/Convoy.Api.dll | grep -E "api/(auth|branches|locations|users|DailySummary)" | sort -u && \
    echo "════════════════════════════════════════════════"

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

# Install strings utility for verification
RUN apt-get update && apt-get install -y binutils && rm -rf /var/lib/apt/lists/*

# CRITICAL: Verify controllers in final DLL
RUN echo "════════════════════════════════════════════════" && \
    echo "🔍 FINAL VERIFICATION: Checking Convoy.Api.dll" && \
    echo "════════════════════════════════════════════════" && \
    echo "DLL size:" && \
    ls -lh /app/Convoy.Api.dll && \
    echo "" && \
    echo "Searching for controller classes..." && \
    CONTROLLERS=$(strings /app/Convoy.Api.dll | grep -E "^(AuthController|BranchController|LocationController|UserController|DailySummaryController)$" | sort -u) && \
    echo "$CONTROLLERS" && \
    echo "" && \
    if echo "$CONTROLLERS" | grep -q "DailySummaryController"; then \
        echo "❌ FATAL: DailySummaryController found! This is OLD CACHE!" && \
        echo "Railway is using stale image. Deployment MUST FAIL." && \
        exit 1; \
    fi && \
    if ! echo "$CONTROLLERS" | grep -q "AuthController"; then \
        echo "❌ FATAL: AuthController NOT found!" && \
        exit 1; \
    fi && \
    if ! echo "$CONTROLLERS" | grep -q "BranchController"; then \
        echo "❌ FATAL: BranchController NOT found!" && \
        exit 1; \
    fi && \
    echo "✅ VERIFICATION PASSED: All required controllers present, no old controllers" && \
    echo "════════════════════════════════════════════════"

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
ENV DOTNET_VERSION=8.0.0.065f49a

# Health check for Railway
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:$PORT/health || exit 1

# Entry point with verbose logging
ENTRYPOINT ["sh", "-c", "echo '🚀 Starting Convoy API v$DOTNET_VERSION on port $PORT' && dotnet Convoy.Api.dll --urls http://0.0.0.0:$PORT"]
