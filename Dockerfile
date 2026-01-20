# Convoy GPS Tracking API - Dockerfile
# Cache bust: 2026-01-20T12:30:00Z - Force rebuild with AuthController and BranchController
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Railway dynamically assigns PORT - don't expose fixed ports
# EXPOSE will be handled by Railway's PORT variable

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Force cache bust - Railway will rebuild from this line
# CRITICAL: This must change on every deploy to invalidate cache
RUN echo "🚀 REBUILD: 1768908340 - FORCE ALL CONTROLLERS" && \
    echo "✅ AuthController (/api/auth)" && \
    echo "✅ BranchController (/api/branches)" && \
    echo "✅ LocationController (/api/locations)" && \
    echo "✅ UserController (/api/users)" && \
    echo "❌ Remove DailySummary (old cache)" && \
    date || true

WORKDIR /src

# Copy csproj files and restore
COPY ["Convoy.Api/Convoy.Api.csproj", "Convoy.Api/"]
COPY ["Convoy.Service/Convoy.Service.csproj", "Convoy.Service/"]
COPY ["Convoy.Data/Convoy.Data.csproj", "Convoy.Data/"]
COPY ["Convoy.Domain/Convoy.Domain.csproj", "Convoy.Domain/"]

RUN dotnet restore "Convoy.Api/Convoy.Api.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/Convoy.Api"
RUN dotnet build "Convoy.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Convoy.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

# Railway provides PORT variable - bind to it
# If PORT not set, default to 8080
ENV PORT=8080

ENTRYPOINT ["sh", "-c", "dotnet Convoy.Api.dll --urls http://0.0.0.0:$PORT"]
