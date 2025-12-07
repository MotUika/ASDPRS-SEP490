# Quick Deploy - Deploy new code to VPS
# Usage: .\deploy\quick-deploy.ps1

$VPS_IP = "160.25.232.199"
$VPS_USER = "root"
$VPS_PASS = "Gm4Gp8mYJGpQ20Jt"
$VPS_PATH = "/opt/asdprs"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Quick Deploy - Code Update Only" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ProjectRoot = Resolve-Path "$PSScriptRoot\.."
Write-Host "[1/4] Preparing files..." -ForegroundColor Yellow
Write-Host "Project: $ProjectRoot"
Write-Host ""

$tempDir = "C:\temp_asdprs_" + (Get-Date -Format "HHmmss")
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

Write-Host "Copying API source code..."
# Copy source code folders
$folders = @("ASDPRS-SEP490", "BussinessObject", "DataAccessLayer", "Repository", "Service")
foreach ($folder in $folders) {
    if (Test-Path "$ProjectRoot\$folder") {
        robocopy "$ProjectRoot\$folder" "$tempDir\$folder" /E /XD "bin" "obj" ".vs" ".git" /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
    }
}

# Copy solution file
Copy-Item "$ProjectRoot\*.sln" $tempDir -ErrorAction SilentlyContinue

Write-Host "Files prepared" -ForegroundColor Green
Write-Host ""

Write-Host "[2/4] Uploading to VPS..." -ForegroundColor Yellow
Write-Host "Password: $VPS_PASS" -ForegroundColor DarkGray
Write-Host ""

# Clean up remote source code directories to prevent duplicates (e.g. migrations)
Write-Host "Cleaning up old source code on VPS..."
ssh "$VPS_USER@$VPS_IP" "rm -rf $VPS_PATH/ASDPRS-SEP490 $VPS_PATH/BussinessObject $VPS_PATH/DataAccessLayer $VPS_PATH/Repository $VPS_PATH/Service"

# Upload source code
# Note: Using scp with recursive flag
scp -r "$tempDir\*" "$VPS_USER@$VPS_IP`:$VPS_PATH/"

Write-Host ""
Write-Host "Upload complete" -ForegroundColor Green
Write-Host ""

Write-Host "[3/4] Rebuilding API..." -ForegroundColor Yellow
Write-Host "Password: $VPS_PASS" -ForegroundColor DarkGray
Write-Host ""

# Clean up Docker garbage before rebuilding to prevent disk full issues
$cleanupCmd = "echo 'Cleaning Docker garbage...' && docker builder prune -a -f && docker system prune -f && echo 'Disk usage:' && df -h / | tail -1"
ssh "$VPS_USER@$VPS_IP" $cleanupCmd

# Rebuild and restart API container
# Using single line command to avoid CRLF issues over SSH
$remoteCmd = "cd $VPS_PATH && echo 'Rebuilding API...' && docker compose -f deploy/docker-compose.yml up -d --build --no-deps api && echo 'Waiting for API...' && sleep 10 && docker compose -f deploy/docker-compose.yml ps"

ssh "$VPS_USER@$VPS_IP" $remoteCmd

Write-Host ""
Write-Host "API rebuilt and restarted" -ForegroundColor Green
Write-Host ""

Write-Host "[4/4] Cleaning up..." -ForegroundColor Yellow
Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
Write-Host "Cleanup complete" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "  Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "API is now running with the latest code!" -ForegroundColor Cyan
Write-Host ""
Write-Host "Check status:" -ForegroundColor Yellow
Write-Host "  https://api.fasm.site" -ForegroundColor White
Write-Host "  https://api.fasm.site/swagger" -ForegroundColor White
Write-Host ""
Write-Host "View logs:" -ForegroundColor Yellow
Write-Host "  ssh $VPS_USER@$VPS_IP" -ForegroundColor White
Write-Host "  docker compose -f $VPS_PATH/deploy/docker-compose.yml logs -f api" -ForegroundColor White
Write-Host ""

