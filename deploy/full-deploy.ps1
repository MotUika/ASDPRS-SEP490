# Full automated deploy from Windows to VPS
# This script uploads AND deploys in one command
# Run: .\deploy\full-deploy.ps1

$VPS_IP = "160.25.232.199"
$VPS_USER = "root"
$VPS_PASS = "Gm4Gp8mYJGpQ20Jt"
$VPS_PATH = "/opt/asdprs"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "ASDPRS Full Deploy to VPS" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "VPS: $VPS_IP"
Write-Host "User: $VPS_USER"
Write-Host "Password: $VPS_PASS"
Write-Host ""

# Get project root
$ScriptPath = $PSScriptRoot
if (-not $ScriptPath) { $ScriptPath = "." }
$ProjectRoot = Split-Path -Parent $ScriptPath
if (-not $ProjectRoot -or $ProjectRoot -eq "") { $ProjectRoot = Get-Location }

Write-Host "Project root: $ProjectRoot"
Write-Host ""

# Step 1: Create temp directory without excluded folders
Write-Host "[1/4] Preparing files..." -ForegroundColor Yellow
$tempDir = "$env:TEMP\asdprs_deploy_$(Get-Date -Format 'yyyyMMddHHmmss')"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

# Copy files excluding .git, bin, obj
$excludeDirs = @('.git', 'bin', 'obj', '.vs', 'node_modules', 'packages')
Get-ChildItem -Path $ProjectRoot -Force | Where-Object {
    $_.Name -notin $excludeDirs
} | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $tempDir -Recurse -Force
}

Write-Host "Files prepared in: $tempDir"

# Step 2: Upload to VPS
Write-Host ""
Write-Host "[2/4] Uploading to VPS..." -ForegroundColor Yellow
Write-Host "Password when prompted: $VPS_PASS" -ForegroundColor Green
Write-Host ""

# Create directory and upload
ssh "$VPS_USER@$VPS_IP" "mkdir -p $VPS_PATH"
scp -r "$tempDir\*" "${VPS_USER}@${VPS_IP}:${VPS_PATH}/"

# Cleanup temp
Remove-Item -Recurse -Force $tempDir

# Step 3: Run deploy on VPS
Write-Host ""
Write-Host "[3/4] Running deploy on VPS..." -ForegroundColor Yellow
Write-Host "Password when prompted: $VPS_PASS" -ForegroundColor Green
Write-Host ""

# Convert line endings using sed, make executable, and run
ssh "$VPS_USER@$VPS_IP" "find $VPS_PATH -type f -name '*.sh' -exec sed -i 's/\r$//' {} + && cd $VPS_PATH/deploy/scripts && chmod +x *.sh && bash ./00-quick-deploy.sh"

# Step 4: Done
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "Deploy Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "API URL: http://$VPS_IP" -ForegroundColor Cyan
Write-Host "Swagger: http://$VPS_IP/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "Management commands (SSH to VPS first):"
Write-Host "  asdprs status   - Check status"
Write-Host "  asdprs logs     - View logs"
Write-Host "  asdprs restart  - Restart services"
Write-Host ""

Read-Host "Press Enter to exit"
