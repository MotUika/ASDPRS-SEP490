# Change SQL Server Password
# New strong password with script to update all configs

$VPS_IP = "160.25.232.199"
$VPS_USER = "root"
$VPS_PASS = "Gm4Gp8mYJGpQ20Jt"
$VPS_PATH = "/opt/asdprs"

# Old and New Passwords
$OLD_PASSWORD = "YourStrong@Passw0rd123"
$NEW_PASSWORD = "Asd#2024!Pr$Sql@Secure890"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Change SQL Server Password" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Old Password: $OLD_PASSWORD" -ForegroundColor Yellow
Write-Host "New Password: $NEW_PASSWORD" -ForegroundColor Green
Write-Host ""

$confirm = Read-Host "Continue? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "Cancelled." -ForegroundColor Red
    exit 0
}

Write-Host ""
Write-Host "[1/5] Updating local config files..." -ForegroundColor Yellow

# Get project root
$ProjectRoot = Split-Path -Parent $PSScriptRoot

# Files to update
$filesToUpdate = @(
    "$ProjectRoot\deploy\docker-compose.yml",
    "$ProjectRoot\deploy\docker-compose-fe-be.yml"
)

foreach ($file in $filesToUpdate) {
    if (Test-Path $file) {
        Write-Host "  Updating: $file"
        (Get-Content $file -Raw) -replace [regex]::Escape($OLD_PASSWORD), $NEW_PASSWORD | Set-Content $file -NoNewline
    }
}

Write-Host "[OK] Local configs updated" -ForegroundColor Green
Write-Host ""

Write-Host "[2/5] Uploading updated docker-compose.yml..." -ForegroundColor Yellow
scp "$ProjectRoot\deploy\docker-compose.yml" "$VPS_USER@$VPS_IP`:$VPS_PATH/deploy/"
Write-Host "[OK] Config uploaded" -ForegroundColor Green
Write-Host ""

Write-Host "[3/5] Changing SA password on SQL Server..." -ForegroundColor Yellow
Write-Host "Password: $VPS_PASS" -ForegroundColor DarkGray
Write-Host ""

ssh "$VPS_USER@$VPS_IP" @"
cd $VPS_PATH
echo 'Changing SQL Server SA password...'
docker exec asdprs-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$OLD_PASSWORD" -C \
  -Q "ALTER LOGIN sa WITH PASSWORD = '$NEW_PASSWORD'"
echo '[OK] Password changed on SQL Server'
"@

Write-Host ""
Write-Host "[OK] SA password changed" -ForegroundColor Green
Write-Host ""

Write-Host "[4/5] Restarting containers with new config..." -ForegroundColor Yellow
ssh "$VPS_USER@$VPS_IP" @"
cd $VPS_PATH
docker compose -f deploy/docker-compose.yml down
docker compose -f deploy/docker-compose.yml up -d
echo 'Waiting for services to be ready...'
sleep 30
docker compose -f deploy/docker-compose.yml ps
"@

Write-Host ""
Write-Host "[OK] Containers restarted" -ForegroundColor Green
Write-Host ""

Write-Host "[5/5] Testing new password..." -ForegroundColor Yellow
ssh "$VPS_USER@$VPS_IP" @"
docker exec asdprs-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$NEW_PASSWORD" -C \
  -Q "SELECT 'Password test: SUCCESS' AS Result"
"@

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Password Changed Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

Write-Host "New SQL Server Credentials:" -ForegroundColor Cyan
Write-Host "  Server: 160.25.232.199,1433" -ForegroundColor White
Write-Host "  User: sa" -ForegroundColor White
Write-Host "  Password: $NEW_PASSWORD" -ForegroundColor Yellow
Write-Host ""

Write-Host "New Connection String:" -ForegroundColor Cyan
Write-Host "  Server=160.25.232.199,1433;Database=LMS_ASDPRS;User Id=sa;Password=$NEW_PASSWORD;TrustServerCertificate=True;" -ForegroundColor White
Write-Host ""

Write-Host "WARNING:" -ForegroundColor Red
Write-Host "  1. Save the new password in a secure location!" -ForegroundColor Yellow
Write-Host "  2. Update connection strings in your applications" -ForegroundColor Yellow
Write-Host "  3. Git commit the updated docker-compose.yml (if you want to version it)" -ForegroundColor Yellow
Write-Host ""
