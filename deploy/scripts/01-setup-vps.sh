#!/bin/bash
# Script 1: Setup VPS Ubuntu 22.04 từ đầu
# Chạy: chmod +x 01-setup-vps.sh && ./01-setup-vps.sh

set -e

echo "=========================================="
echo "Step 1: Setup VPS Ubuntu 22.04"
echo "=========================================="

# Update system
echo "[1/7] Updating system packages..."
apt-get update && apt-get upgrade -y

# Install essential packages
echo "[2/7] Installing essential packages..."
apt-get install -y \
    ca-certificates \
    curl \
    gnupg \
    lsb-release \
    wget \
    unzip \
    git \
    htop \
    nano \
    ufw

# Setup Docker repository
echo "[3/7] Setting up Docker repository..."
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg

echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null

# Install Docker
echo "[4/7] Installing Docker..."
apt-get update
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Start and enable Docker
echo "[5/7] Starting Docker service..."
systemctl start docker
systemctl enable docker

# Configure firewall
echo "[6/7] Configuring firewall..."
ufw --force reset
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp    # SSH
ufw allow 80/tcp    # HTTP
ufw allow 443/tcp   # HTTPS
ufw allow 5000/tcp  # API direct (optional)
ufw --force enable

# Create app directory
echo "[7/7] Creating application directory..."
mkdir -p /opt/asdprs
mkdir -p /opt/asdprs/data/sqlserver
mkdir -p /opt/asdprs/logs

echo ""
echo "=========================================="
echo "VPS Setup Complete!"
echo "=========================================="
echo "Docker version: $(docker --version)"
echo "Docker Compose version: $(docker compose version)"
echo ""
echo "Next step: Run 02-deploy-app.sh"
