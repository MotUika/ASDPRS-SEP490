#!/bin/bash
# Quick setup script for api.fasm.site
# Run this on VPS after uploading new nginx.conf

set -e

echo "=========================================="
echo "Setup API Domain: api.fasm.site"
echo "=========================================="
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (or use sudo)"
    exit 1
fi

DOMAIN="api.fasm.site"
PROJECT_DIR="/opt/asdprs"

echo "[1/5] Checking DNS..."
DNS_IP=$(dig +short $DOMAIN | head -n1)
if [ "$DNS_IP" != "160.25.232.199" ]; then
    echo "⚠️  Warning: DNS not pointing to this server yet"
    echo "Current: $DNS_IP"
    echo "Expected: 160.25.232.199"
    echo ""
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
else
    echo "✅ DNS OK: $DOMAIN → $DNS_IP"
fi

echo ""
echo "[2/5] Updating Nginx config..."
cd $PROJECT_DIR

# Backup current config
if [ -f deploy/nginx.conf ]; then
    cp deploy/nginx.conf deploy/nginx.conf.backup.$(date +%Y%m%d_%H%M%S)
    echo "✅ Backed up old config"
fi

# Restart nginx to apply new config
echo ""
echo "[3/5] Restarting Nginx..."
docker compose -f deploy/docker-compose.yml restart nginx

# Wait for nginx to be ready
sleep 3

# Check nginx status
if docker ps | grep -q asdprs-nginx; then
    echo "✅ Nginx restarted successfully"
else
    echo "❌ Nginx failed to start"
    echo "Check logs: docker logs asdprs-nginx"
    exit 1
fi

echo ""
echo "[4/5] Installing Certbot (if not exists)..."
if ! command -v certbot &> /dev/null; then
    apt-get update
    apt-get install -y certbot python3-certbot-nginx
    echo "✅ Certbot installed"
else
    echo "✅ Certbot already installed"
fi

echo ""
echo "[5/5] Setting up SSL for $DOMAIN..."
echo ""
echo "This will:"
echo "  1. Get SSL certificate from Let's Encrypt"
echo "  2. Configure Nginx for HTTPS"
echo "  3. Setup auto-renewal"
echo ""

# Stop nginx temporarily for certbot standalone mode
docker compose -f deploy/docker-compose.yml stop nginx

# Get certificate
certbot certonly --standalone \
    -d $DOMAIN \
    --non-interactive \
    --agree-tos \
    --email admin@$DOMAIN \
    --preferred-challenges http

if [ $? -eq 0 ]; then
    echo "✅ SSL certificate obtained successfully"
    
    # Create SSL directory
    mkdir -p $PROJECT_DIR/deploy/ssl
    
    # Copy certificates
    cp /etc/letsencrypt/live/$DOMAIN/fullchain.pem $PROJECT_DIR/deploy/ssl/
    cp /etc/letsencrypt/live/$DOMAIN/privkey.pem $PROJECT_DIR/deploy/ssl/
    
    echo "✅ Certificates copied to deploy/ssl/"
    
    # Update nginx config for HTTPS
    cat > $PROJECT_DIR/deploy/nginx.conf << 'EOF'
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
        server_name api.fasm.site;
        return 301 https://$server_name$request_uri;
    }

    # HTTPS Server
    server {
        listen 443 ssl http2;
        server_name api.fasm.site;

        # SSL Configuration
        ssl_certificate /etc/nginx/ssl/fullchain.pem;
        ssl_certificate_key /etc/nginx/ssl/privkey.pem;
        
        # SSL Security
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;
        ssl_prefer_server_ciphers on;

        # API endpoints
        location / {
            proxy_pass http://api;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_cache_bypass $http_upgrade;
            
            # Timeout settings
            proxy_read_timeout 300;
            proxy_send_timeout 300;
            proxy_connect_timeout 300;
        }

        # SignalR WebSocket
        location /notificationHub {
            proxy_pass http://api;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            
            # Long timeout for WebSocket
            proxy_read_timeout 86400;
            proxy_send_timeout 86400;
        }
    }
}
EOF
    
    echo "✅ Nginx config updated for HTTPS"
    
    # Setup auto-renewal
    echo "0 0,12 * * * root certbot renew --quiet --deploy-hook 'docker restart asdprs-nginx'" > /etc/cron.d/certbot-renew
    echo "✅ Auto-renewal configured"
    
else
    echo "❌ Failed to obtain SSL certificate"
    echo ""
    echo "Possible reasons:"
    echo "  1. DNS not propagated yet (wait 15-30 minutes)"
    echo "  2. Port 80 blocked by firewall"
    echo "  3. Domain not pointing to this server"
    echo ""
    echo "Try again later or check: certbot certificates"
fi

# Start nginx again
docker compose -f deploy/docker-compose.yml start nginx

echo ""
echo "=========================================="
echo "Setup Complete!"
echo "=========================================="
echo ""
echo "Your API is now available at:"
echo "  🔓 HTTP:  http://api.fasm.site (redirects to HTTPS)"
echo "  🔒 HTTPS: https://api.fasm.site"
echo "  📚 Swagger: https://api.fasm.site/swagger"
echo ""
echo "Check status:"
echo "  docker compose -f deploy/docker-compose.yml ps"
echo ""
echo "View logs:"
echo "  docker logs asdprs-nginx -f"
echo ""
