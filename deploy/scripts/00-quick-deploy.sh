#!/bin/bash
# Quick Deploy Script - Chạy tất cả từ đầu
# Upload project lên VPS rồi chạy script này
# Usage: chmod +x 00-quick-deploy.sh && ./00-quick-deploy.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=========================================="
echo "ASDPRS Quick Deploy"
echo "=========================================="
echo ""

# Make all scripts executable
chmod +x $SCRIPT_DIR/*.sh

# Step 1: Setup VPS
echo "Running VPS setup..."
$SCRIPT_DIR/01-setup-vps.sh

# Step 2: Deploy application
echo ""
echo "Running application deployment..."
$SCRIPT_DIR/02-deploy-app.sh

# Copy management script to /usr/local/bin for easy access
cp $SCRIPT_DIR/03-manage-services.sh /usr/local/bin/asdprs
chmod +x /usr/local/bin/asdprs

echo ""
echo "=========================================="
echo "Quick Deploy Complete!"
echo "=========================================="
echo ""
echo "Your API is now running at:"
echo "  - http://160.25.232.199"
echo "  - http://160.25.232.199/swagger"
echo ""
echo "Management commands (run from anywhere):"
echo "  asdprs status   - Check service status"
echo "  asdprs logs     - View logs"
echo "  asdprs restart  - Restart services"
echo "  asdprs rebuild  - Rebuild and restart API"
echo ""
echo "For SSL setup, run: ./05-ssl-setup.sh your-domain.com"
