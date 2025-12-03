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

# Get project root
$ProjectRoot = Split-Path -Parent $PSScriptRoot

Write-Host "[1/4] Preparing files..." -ForegroundColor Yellow
Write-Host "Project: $ProjectRoot"
Write-Host ""

# Create temp folder with short path
$tempDir = "C:\temp_asdprs_$(Get-Date -Format 'HHmmss')"
if (Test-Path $tempDir) {
    robocopy $tempDir $tempDir /PURGE /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
    Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
}

# Copy only necessary folders for API
Write-Host "Copying API source code..."
robocopy "$ProjectRoot\ASDPRS-SEP490" "$tempDir\ASDPRS-SEP490" /E /XD bin obj /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
robocopy "$ProjectRoot\BussinessObject" "$tempDir\BussinessObject" /E /XD bin obj /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
robocopy "$ProjectRoot\DataAccessLayer" "$tempDir\DataAccessLayer" /E /XD bin obj /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
robocopy "$ProjectRoot\Repository" "$tempDir\Repository" /E /XD bin obj /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
robocopy "$ProjectRoot\Service" "$tempDir\Service" /E /XD bin obj /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null

# Copy solution file
Copy-Item "$ProjectRoot\*.sln" $tempDir -ErrorAction SilentlyContinue

Write-Host "✓ Files prepared" -ForegroundColor Green
Write-Host ""

Write-Host "[2/4] Uploading to VPS..." -ForegroundColor Yellow
Write-Host "Password: $VPS_PASS" -ForegroundColor DarkGray
Write-Host ""

# Upload source code
scp -r "$tempDir\*" "$VPS_USER@$VPS_IP`:$VPS_PATH/"

Write-Host ""
Write-Host "✓ Upload complete" -ForegroundColor Green
Write-Host ""

Write-Host "[3/4] Rebuilding API..." -ForegroundColor Yellow
Write-Host "Password: $VPS_PASS" -ForegroundColor DarkGray
Write-Host ""

# Rebuild and restart API container
ssh "$VPS_USER@$VPS_IP" @"
cd $VPS_PATH
echo 'Rebuilding API container...'
docker compose -f deploy/docker-compose.yml up -d --build --no-deps api
echo 'Waiting for API to be ready...'
sleep 10
docker compose -f deploy/docker-compose.yml ps
"@

Write-Host ""
Write-Host "✓ API rebuilt and restarted" -ForegroundColor Green
Write-Host ""

Write-Host "[4/4] Cleaning up..." -ForegroundColor Yellow
robocopy $tempDir $tempDir /PURGE /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
Write-Host "✓ Cleanup complete" -ForegroundColor Green
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
