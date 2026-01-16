#!/bin/bash
# Production deployment script

set -e

echo "================================================"
echo "Convoy GPS Tracking - Production Deployment"
echo "================================================"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check Docker
if ! command -v docker &> /dev/null; then
    echo "Docker not installed!"
    exit 1
fi

# Check .env
if [ ! -f .env ]; then
    echo "${YELLOW}Warning: .env not found. Copying from template...${NC}"
    cp .env.example .env
    echo "${YELLOW}Please edit .env with production values!${NC}"
    read -p "Press enter to continue or Ctrl+C to exit..."
fi

# Pull latest code
echo "${GREEN}[1/5] Pulling latest code...${NC}"
git pull origin main

# Build images
echo "${GREEN}[2/5] Building Docker images...${NC}"
docker-compose -f docker-compose.prod.yml build

# Stop old containers
echo "${GREEN}[3/5] Stopping old containers...${NC}"
docker-compose -f docker-compose.prod.yml down

# Start new containers
echo "${GREEN}[4/5] Starting new containers...${NC}"
docker-compose -f docker-compose.prod.yml up -d

# Health check
echo "${GREEN}[5/5] Running health check...${NC}"
sleep 10

API_PORT=$(grep API_PORT .env | cut -d '=' -f2)
API_PORT=${API_PORT:-8080}

if curl -f http://localhost:$API_PORT/api/location/user/1/last?count=1 > /dev/null 2>&1; then
    echo "${GREEN}✅ Deployment successful!${NC}"
    echo "API: http://localhost:$API_PORT/swagger"
else
    echo "${YELLOW}⚠️  API not responding. Check logs:${NC}"
    echo "docker-compose -f docker-compose.prod.yml logs -f api"
fi

echo ""
echo "================================================"
echo "Useful commands:"
echo "  docker-compose -f docker-compose.prod.yml logs -f"
echo "  docker-compose -f docker-compose.prod.yml ps"
echo "  docker-compose -f docker-compose.prod.yml down"
echo "================================================"
