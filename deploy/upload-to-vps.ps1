# Upload project to VPS from Windows PowerShell
# Run: .\deploy\upload-to-vps.ps1

$VPS_IP = "160.25.232.199"
$VPS_USER = "root"
$VPS_PASS = "Gm4Gp8mYJGpQ20Jt"
$VPS_PATH = "/opt/asdprs"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Upload ASDPRS to VPS" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Get project root directory (ASDPRS-SEP490 folder)
# Script is in: C:\Working\ASDPRS-SEP490\deploy\upload-to-vps.ps1
# $PSScriptRoot = C:\Working\ASDPRS-SEP490\deploy
# $ProjectRoot should be C:\Working\ASDPRS-SEP490
$ProjectRoot = Split-Path -Parent $PSScriptRoot

if (-not $ProjectRoot -or -not (Test-Path $ProjectRoot)) {
    Write-Host "Error: Could not determine project root" -ForegroundColor Red
    exit 1
}

Write-Host "Project: $ProjectRoot"
Write-Host "Target: $VPS_USER@$VPS_IP`:$VPS_PATH"
Write-Host ""

# Check if scp is available
$scpPath = Get-Command scp -ErrorAction SilentlyContinue

if ($scpPath) {
    Write-Host "Using SCP to upload..." -ForegroundColor Yellow
    Write-Host "You will need to enter password: $VPS_PASS" -ForegroundColor Green
    Write-Host ""
    
    # Create directory on VPS first
    Write-Host "Creating directory on VPS..."
    ssh "$VPS_USER@$VPS_IP" "mkdir -p $VPS_PATH"
    
    # Upload files (excluding unnecessary folders)
    
    # Use shorter temp path to avoid Windows path length limit (260 chars)
    # Flutter projects have very deep nested folders
    $tempDir = "C:\temp_asdprs_$(Get-Date -Format 'HHmmss')"
    if (Test-Path $tempDir) { 
        # Use robocopy to delete (handles long paths better)
        robocopy $tempDir $tempDir /PURGE /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
        Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
    }
    
    # Copy files excluding unnecessary folders
    Write-Host "Copying project files to temp folder..." -ForegroundColor Yellow
    Write-Host "From: $ProjectRoot"
    Write-Host "To: $tempDir"
    Write-Host ""
    robocopy $ProjectRoot $tempDir /E /XD .git bin obj .vs node_modules /NFL /NDL /NJH /NJS /NC /NS /NP
    
    # Upload
    scp -r "$tempDir\*" "$VPS_USER@$VPS_IP`:$VPS_PATH/"
    
    # Cleanup
    Write-Host ""
    Write-Host "Cleaning up temp folder..." -ForegroundColor Yellow
    robocopy $tempDir $tempDir /PURGE /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
    Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
    
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host "Upload Complete!" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
}
else {
    Write-Host "SCP not found. Please install OpenSSH or Git for Windows." -ForegroundColor Red
    Write-Host ""
    Write-Host "Install options:" -ForegroundColor Yellow
    Write-Host "1. Enable OpenSSH in Windows Features"
    Write-Host "2. Install Git for Windows (includes ssh/scp)"
    Write-Host ""
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. SSH to VPS:"
Write-Host "   ssh $VPS_USER@$VPS_IP" -ForegroundColor Yellow
Write-Host "   Password: $VPS_PASS" -ForegroundColor Green
Write-Host ""
Write-Host "2. Run deploy:"
Write-Host "   cd $VPS_PATH/deploy/scripts" -ForegroundColor Yellow
Write-Host "   chmod +x *.sh && ./00-quick-deploy.sh" -ForegroundColor Yellow
Write-Host ""

Read-Host "Press Enter to exit"
