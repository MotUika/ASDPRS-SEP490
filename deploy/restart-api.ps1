# Quick Restart API (no rebuild)
# Usage: .\deploy\restart-api.ps1

$VPS_IP = "160.25.232.199"
$VPS_USER = "root"
$VPS_PASS = "Gm4Gp8mYJGpQ20Jt"
$VPS_PATH = "/opt/asdprs"

Write-Host ""
Write-Host "Restarting API on VPS..." -ForegroundColor Cyan
Write-Host "Password: $VPS_PASS" -ForegroundColor DarkGray
Write-Host ""

ssh "$VPS_USER@$VPS_IP" "docker restart asdprs-api && sleep 5 && docker ps | grep asdprs"

Write-Host ""
Write-Host "✓ API restarted!" -ForegroundColor Green
Write-Host ""
Write-Host "Check: https://api.fasm.site/swagger" -ForegroundColor Yellow
Write-Host ""
