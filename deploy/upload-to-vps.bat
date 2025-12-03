@echo off
REM Upload project to VPS from Windows
REM Requires: Install scp via Git Bash or use PowerShell

set VPS_IP=160.25.232.199
set VPS_USER=root
set VPS_PASS=Gm4Gp8mYJGpQ20Jt
set VPS_PATH=/opt/asdprs

echo ==========================================
echo Upload ASDPRS to VPS
echo ==========================================
echo.

REM Check if pscp exists (PuTTY)
where pscp >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo Using PSCP...
    echo %VPS_PASS%| pscp -r -pw %VPS_PASS% -batch . %VPS_USER%@%VPS_IP%:%VPS_PATH%
    goto :done
)

REM Fallback to scp (requires Git Bash or WSL)
echo PSCP not found. Please install PuTTY or use Git Bash.
echo.
echo Option 1: Install PuTTY from https://www.putty.org/
echo Option 2: Run this in Git Bash:
echo    sshpass -p '%VPS_PASS%' scp -r . %VPS_USER%@%VPS_IP%:%VPS_PATH%
echo.
echo Option 3: Manual upload with password prompt:
echo    scp -r . %VPS_USER%@%VPS_IP%:%VPS_PATH%
echo    Password: %VPS_PASS%
pause
exit /b 1

:done
echo.
echo Upload complete!
echo.
echo Next: SSH to VPS and run deploy
echo    ssh %VPS_USER%@%VPS_IP%
echo    Password: %VPS_PASS%
echo    cd %VPS_PATH%/deploy/scripts
echo    chmod +x *.sh ^&^& ./00-quick-deploy.sh
pause
