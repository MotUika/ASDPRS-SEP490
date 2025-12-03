#!/bin/bash
# Open SQL Server port 1433 for external connections
# Run on VPS as root

set -e

echo "=========================================="
echo "Open SQL Server Port 1433"
echo "=========================================="
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root"
    exit 1
fi

echo "[1/3] Checking current firewall status..."
ufw status
echo ""

echo "[2/3] Opening port 1433 (SQL Server)..."
ufw allow 1433/tcp
echo "✓ Port 1433 opened"
echo ""

echo "[3/3] Verifying..."
ufw status | grep 1433
echo ""

echo "=========================================="
echo "Port 1433 Opened Successfully!"
echo "=========================================="
echo ""

echo "You can now connect to SQL Server from external machines:"
echo ""
echo "Server: 160.25.232.199,1433"
echo "User: sa"
echo "Password: YourStrong@Passw0rd123"
echo "Database: LMS_ASDPRS"
echo ""
echo "Connection String:"
echo "Server=160.25.232.199,1433;Database=LMS_ASDPRS;User Id=sa;Password=YourStrong@Passw0rd123;TrustServerCertificate=True;"
echo ""
echo "⚠️  Security Note:"
echo "  - Consider restricting access to specific IP addresses"
echo "  - Change the default SA password"
echo "  - Use VPN or SSH tunnel for better security"
echo ""
