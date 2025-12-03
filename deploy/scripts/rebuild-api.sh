#!/bin/bash
# Quick rebuild API from VPS
# Usage: ./rebuild-api.sh

set -e

VPS_PATH="/opt/asdprs"

echo ""
echo "=========================================="
echo "  Quick Rebuild API"
echo "=========================================="
echo ""

cd $VPS_PATH

echo "[1/3] Stopping API..."
docker compose -f deploy/docker-compose.yml stop api
echo "✓ API stopped"
echo ""

echo "[2/3] Rebuilding API..."
docker compose -f deploy/docker-compose.yml build --no-cache api
echo "✓ API built"
echo ""

echo "[3/3] Starting API..."
docker compose -f deploy/docker-compose.yml up -d api
echo "✓ API started"
echo ""

echo "Waiting for API to be ready..."
sleep 10
echo ""

echo "=========================================="
echo "  Rebuild Complete!"
echo "=========================================="
echo ""

echo "Container status:"
docker compose -f deploy/docker-compose.yml ps

echo ""
echo "API health:"
curl -s -o /dev/null -w "%{http_code}" http://localhost:5000 || echo "Checking..."

echo ""
echo ""
echo "View logs:"
echo "  docker compose -f deploy/docker-compose.yml logs -f api"
echo ""
