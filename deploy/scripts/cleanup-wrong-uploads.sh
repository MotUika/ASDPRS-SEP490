#!/bin/bash
# Script to clean up wrongly uploaded files on VPS
# Keep only ASDPRS-SEP490 project files

set -e

echo "=========================================="
echo "Cleanup Wrongly Uploaded Files"
echo "=========================================="
echo ""

VPS_PATH="/opt/asdprs"

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (or use sudo)"
    exit 1
fi

echo "⚠️  WARNING: This will delete files/folders that don't belong to ASDPRS-SEP490"
echo ""
echo "Files/folders to DELETE:"
echo "  - backend/"
echo "  - CodeNhanTuongHoc/"
echo "  - CLIPROXYAPI/"
echo "  - lasotuvi/"
echo "  - All .exe, .tar files"
echo "  - All deployment scripts from other projects"
echo ""
echo "Files/folders to KEEP:"
echo "  - ASDPRS-SEP490/"
echo "  - BussinessObject/"
echo "  - DataAccessLayer/"
echo "  - Repository/"
echo "  - Service/"
echo "  - deploy/"
echo "  - *.sln, *.csproj files"
echo ""

read -p "Continue? (yes/no): " -r
if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo "Cancelled."
    exit 0
fi

echo ""
echo "Starting cleanup..."
cd $VPS_PATH

# List of folders/files to DELETE (from other projects)
FOLDERS_TO_DELETE=(
    "backend"
    "CodeNhanTuongHoc"
    "CLIPROXYAPI"
    "lasotuvi"
    "node_modules"
    ".github"
    "prisma"
    "src"
    "dist"
    "build"
    "tests"
    "docs"
    "scripts"
    "public"
    "static"
    "templates"
)

FILES_TO_DELETE=(
    "*.exe"
    "*.tar"
    "*.log"
    "cli-proxy-api*"
    "backend.tar"
    "package.json"
    "package-lock.json"
    "tsconfig.json"
    "nodemon.json"
    "docker-compose.local.yml"
    "docker-compose.prod.yml"
    "docker-compose.prod-open.yml"
    "docker-compose.prod.FINAL.yml"
    "Dockerfile"
    ".dockerignore"
    ".env"
    ".env.production"
    ".env.production.example"
    ".env.production.FINAL"
    "go.mod"
    "go.sum"
    "main.go"
    "requirements.txt"
    "requirements.prod.txt"
    "alembic.ini"
    ".travis.yml"
    "setup.py"
    "setup.cfg"
    "LICENSE"
    "README.md"
    "*.md"
)

# Delete folders
echo ""
echo "[1/3] Deleting unwanted folders..."
for folder in "${FOLDERS_TO_DELETE[@]}"; do
    if [ -d "$folder" ]; then
        echo "  Deleting: $folder/"
        rm -rf "$folder"
    fi
done

# Delete files
echo ""
echo "[2/3] Deleting unwanted files..."
for pattern in "${FILES_TO_DELETE[@]}"; do
    # Find and delete matching files (excluding deploy folder and project folders)
    find . -maxdepth 1 -name "$pattern" -type f -exec rm -f {} \; 2>/dev/null || true
done

# Delete specific deployment scripts from other projects
echo ""
echo "[3/3] Cleaning up deployment scripts..."
DEPLOY_SCRIPTS_TO_DELETE=(
    "build-and-deploy.ps1"
    "build-and-deploy-simple.ps1"
    "build-and-push-ghcr.ps1"
    "build-and-push-images.ps1"
    "build-for-deploy.ps1"
    "build.ps1"
    "check-vps-status.ps1"
    "connect-vps.ps1"
    "deploy-on-vps.sh"
    "deploy-to-vps.ps1"
    "deploy-vps.sh"
    "deploy.ps1"
    "setup-vps.sh"
    "ssh-to-vps.ps1"
    "ssh-tunnel.ps1"
    "upload-dist-to-vps.ps1"
    "verify-deployment.ps1"
)

for script in "${DEPLOY_SCRIPTS_TO_DELETE[@]}"; do
    if [ -f "$script" ]; then
        echo "  Deleting: $script"
        rm -f "$script"
    fi
done

# Clean up any .pyc, .js.map files
find . -name "*.pyc" -delete 2>/dev/null || true
find . -name "*.js.map" -delete 2>/dev/null || true
find . -name "__pycache__" -type d -exec rm -rf {} + 2>/dev/null || true

echo ""
echo "=========================================="
echo "Cleanup Complete!"
echo "=========================================="
echo ""
echo "Remaining structure:"
ls -la $VPS_PATH

echo ""
echo "Disk usage:"
du -sh $VPS_PATH

echo ""
echo "✅ Done! Only ASDPRS-SEP490 project files remain."
echo ""
