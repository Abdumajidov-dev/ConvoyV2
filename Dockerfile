# Convoy GPS Tracking API - Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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

# Railway will inject PORT environment variable at runtime
# ASPNETCORE_URLS will be set via Railway environment variables as: http://0.0.0.0:$PORT

ENTRYPOINT ["dotnet", "Convoy.Api.dll"]
