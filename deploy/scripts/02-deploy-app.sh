#!/bin/bash
# Script 2: Deploy ASDPRS Application
# Chạy: chmod +x 02-deploy-app.sh && ./02-deploy-app.sh

set -e

APP_DIR="/opt/asdprs"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$(dirname "$SCRIPT_DIR")")"

echo "=========================================="
echo "Step 2: Deploy ASDPRS Application"
echo "=========================================="

# Check if Docker is running
if ! systemctl is-active --quiet docker; then
    echo "Error: Docker is not running. Please run 01-setup-vps.sh first."
    exit 1
fi

# Navigate to project directory
cd "$PROJECT_DIR"

echo "[1/5] Stopping existing containers..."
docker compose -f deploy/docker-compose.yml down --remove-orphans 2>/dev/null || true

echo "[2/5] Pulling latest base images..."
docker pull mcr.microsoft.com/dotnet/sdk:8.0
docker pull mcr.microsoft.com/dotnet/aspnet:8.0
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker pull nginx:alpine

echo "[3/5] Building application..."
docker compose -f deploy/docker-compose.yml build --no-cache

echo "[4/5] Starting services..."
docker compose -f deploy/docker-compose.yml up -d

echo "[5/5] Waiting for services to be healthy..."
sleep 10

# Check SQL Server health
echo "Checking SQL Server..."
for i in {1..30}; do
    if docker exec asdprs-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd123" -C -Q "SELECT 1" &>/dev/null; then
        echo "SQL Server is ready!"
        break
    fi
    echo "Waiting for SQL Server... ($i/30)"
    sleep 5
done

# Check API health
echo "Checking API..."
for i in {1..20}; do
    if curl -s http://localhost:5000 > /dev/null 2>&1; then
        echo "API is ready!"
        break
    fi
    echo "Waiting for API... ($i/20)"
    sleep 3
done

echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
docker compose -f deploy/docker-compose.yml ps
echo ""
echo "API URL: http://160.25.232.199"
echo "Swagger: http://160.25.232.199/swagger"
echo ""
echo "View logs: docker compose -f deploy/docker-compose.yml logs -f"
