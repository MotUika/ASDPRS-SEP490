#!/bin/bash
# Install Full-Text Search on SQL Server 2022 (Ubuntu 22.04)
# Fixes "package not found" by adding Microsoft repo

set -e

echo "=========================================="
echo "Install SQL Server Full-Text Search (Retry)"
echo "=========================================="
echo ""

echo "[0/4] Fixing potential broken installs..."
docker exec -u 0 asdprs-sqlserver bash -c "dpkg --configure -a || true"

echo ""
echo "[1/4] Adding Microsoft Repository..."
docker exec -u 0 asdprs-sqlserver bash -c "
  apt-get update && \
  apt-get install -y curl gnupg && \
  curl -fsSL https://packages.microsoft.com/keys/microsoft.asc -o /tmp/microsoft.asc && \
  gpg --batch --yes --dearmor -o /usr/share/keyrings/microsoft-prod.gpg /tmp/microsoft.asc && \
  curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list | tee /etc/apt/sources.list.d/mssql-server-2022.list > /dev/null
"

echo ""
echo "[2/4] Installing mssql-server-fts..."
docker exec -u 0 asdprs-sqlserver bash -c "
  apt-get update && \
  apt-get install -y mssql-server-fts
"

echo ""
echo "[3/4] Restarting SQL Server container..."
docker restart asdprs-sqlserver

echo ""
echo "[4/4] Verifying installation..."
sleep 15 # Wait for SQL Server to start

docker exec asdprs-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'Asd2025!PrSqlSecure890' -C \
  -Q "SELECT CASE WHEN FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1 THEN 'INSTALLED' ELSE 'NOT INSTALLED' END AS Status"

echo ""
echo "=========================================="
echo "FTS Installation Complete!"
echo "=========================================="
echo ""
