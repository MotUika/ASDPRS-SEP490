#!/bin/bash
# Script 5: Setup SSL với Let's Encrypt (Optional)
# Usage: ./05-ssl-setup.sh [domain]
# Example: ./05-ssl-setup.sh api.example.com

set -e

DOMAIN=$1

if [ -z "$DOMAIN" ]; then
    echo "Usage: $0 [domain]"
    echo "Example: $0 api.example.com"
    echo ""
    echo "Note: You need a domain pointing to this server's IP (160.25.232.199)"
    exit 1
fi

echo "=========================================="
echo "Setting up SSL for: $DOMAIN"
echo "=========================================="

# Install Certbot
echo "[1/4] Installing Certbot..."
apt-get update
apt-get install -y certbot

# Stop nginx temporarily
echo "[2/4] Stopping nginx..."
docker stop asdprs-nginx 2>/dev/null || true

# Get certificate
echo "[3/4] Obtaining SSL certificate..."
certbot certonly --standalone -d $DOMAIN --non-interactive --agree-tos --email admin@$DOMAIN

# Create SSL directory and copy certs
echo "[4/4] Configuring SSL..."
mkdir -p /opt/asdprs/deploy/ssl
cp /etc/letsencrypt/live/$DOMAIN/fullchain.pem /opt/asdprs/deploy/ssl/
cp /etc/letsencrypt/live/$DOMAIN/privkey.pem /opt/asdprs/deploy/ssl/

# Update nginx config for SSL
cat > /opt/asdprs/deploy/nginx-ssl.conf << EOF
events {
    worker_connections 1024;
}

http {
    upstream api {
        server api:80;
    }

    # Redirect HTTP to HTTPS
    server {
        listen 80;
        server_name $DOMAIN;
        return 301 https://\$server_name\$request_uri;
    }

    # HTTPS server
    server {
        listen 443 ssl http2;
        server_name $DOMAIN;

        ssl_certificate /etc/nginx/ssl/fullchain.pem;
        ssl_certificate_key /etc/nginx/ssl/privkey.pem;
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;

        location / {
            proxy_pass http://api;
            proxy_http_version 1.1;
            proxy_set_header Upgrade \$http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host \$host;
            proxy_set_header X-Real-IP \$remote_addr;
            proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto \$scheme;
            proxy_cache_bypass \$http_upgrade;
            proxy_read_timeout 86400;
            proxy_send_timeout 86400;
        }

        location /notificationHub {
            proxy_pass http://api;
            proxy_http_version 1.1;
            proxy_set_header Upgrade \$http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host \$host;
            proxy_set_header X-Real-IP \$remote_addr;
            proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto \$scheme;
            proxy_read_timeout 86400;
        }
    }
}
EOF

# Restart nginx with new config
docker start asdprs-nginx

echo ""
echo "=========================================="
echo "SSL Setup Complete!"
echo "=========================================="
echo "Your API is now available at: https://$DOMAIN"
echo ""
echo "Certificate will expire in 90 days."
echo "To renew: certbot renew"
